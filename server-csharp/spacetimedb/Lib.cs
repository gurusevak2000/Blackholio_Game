// Lib.cs — Blackholio Server Module
// Purpose: All SpacetimeDB tables and reducers for the game
// API: SpacetimeDB 2.0 (breaking changes from 1.x)
// Last updated: 2025-05-18

using SpacetimeDB;

public static partial class Module
{
    // ─────────────────────────────────────────────
    // CUSTOM TYPES
    // ─────────────────────────────────────────────

    [SpacetimeDB.Type]
    public partial struct DbVector2
    {
        public float X;
        public float Y;
        public DbVector2(float x, float y) { X = x; Y = y; }
    }

    // ─────────────────────────────────────────────
    // TABLES
    // ─────────────────────────────────────────────

    // Scheduled table — triggers SpawnFood every 500ms
    // FIX: 2.0 uses Accessor = (PascalCase) not Name = (snake_case) for the DB accessor
    [Table(Accessor = "SpawnFoodTimer",
                       Scheduled = nameof(SpawnFood),
                       ScheduledAt = nameof(ScheduledAt))]
    public partial struct SpawnFoodTimer
    {
        [PrimaryKey, AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt; // 2.0: ScheduleAt (not ScheduledAt from SpacetimeDB ns)
    }

    [SpacetimeDB.Table(Accessor = "Config", Public = true)]
    public partial struct Config
    {
        [PrimaryKey]
        public uint Id; // 2.0: PrimaryKey must be a numeric or Identity type, not string
        public uint WorldSize;
    }

    [SpacetimeDB.Table(Accessor = "Entity", Public = true)]
    public partial struct Entity
    {
        [PrimaryKey, AutoInc]
        public uint EntityId;
        public DbVector2 Position;
        public uint Mass;
    }

    [SpacetimeDB.Table(Accessor = "Circle", Public = true)]
    public partial struct Circle
    {
        [PrimaryKey]
        public uint EntityId;
        [SpacetimeDB.Index.BTree]
        public uint PlayerId;
        public DbVector2 Direction;
        public float Speed;
        public long LastSplitTime;
    }

    [SpacetimeDB.Table(Accessor = "Food", Public = true)]
    public partial struct Food
    {
        [PrimaryKey]
        public uint EntityId;
    }

    // 2.0: Two separate [Table] attributes on one struct is NOT supported in 2.0.
    // Use two separate structs instead — one for online players, one for logged-out.
    [SpacetimeDB.Table(Accessor = "Player", Public = true)]
    public partial struct Player
    {
        [PrimaryKey]
        public Identity Identity;
        [Unique, AutoInc]
        public uint PlayerId;
        public string Name;   // FIX: was ulong in old code — name should be a string
    }

    [SpacetimeDB.Table(Accessor = "LoggedOutPlayer")] // private — not Public
    public partial struct LoggedOutPlayer
    {
        [PrimaryKey]
        public Identity Identity;
        [Unique, AutoInc]
        public uint PlayerId;
        public string Name;
    }

    // ─────────────────────────────────────────────
    // CONSTANTS & HELPERS
    // ─────────────────────────────────────────────

    const uint FOOD_MASS_MIN = 2;
    const uint FOOD_MASS_MAX = 4;
    const uint TARGET_FOOD_COUNT = 600;
    const uint START_PLAYER_MASS = 15;

    public static float MassToRadius(uint mass) => MathF.Sqrt(mass);

    // Extension helpers for Random (same as original — still valid in 2.0)
    public static float Range(this Random rng, float min, float max)
        => rng.NextSingle() * (max - min) + min;
    public static uint Range(this Random rng, uint min, uint max)
        => (uint)rng.NextInt64(min, max);

    // ─────────────────────────────────────────────
    // LIFECYCLE REDUCERS
    // ─────────────────────────────────────────────

    [SpacetimeDB.Reducer(ReducerKind.Init)]
    public static void Init(ReducerContext ctx)
    {
        Log.Info("Init...");

        // Insert config row with Id = 0
        ctx.Db.Config.Insert(new Config
        {
            Id = 0,
            WorldSize = 1000
        });

        // Schedule food spawner to run every 500ms
        ctx.Db.SpawnFoodTimer.Insert(new SpawnFoodTimer
        {
            ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromMilliseconds(500))
        });
    }

    [SpacetimeDB.Reducer(ReducerKind.ClientConnected)]
    public static void Connect(ReducerContext ctx)
    {
        // Check if this player was previously logged out
        var player = ctx.Db.LoggedOutPlayer.Identity.Find(ctx.Sender);
        if (player != null)
        {
            // Re-insert into active player table, remove from logged-out
            ctx.Db.Player.Insert(new Player
            {
                Identity = player.Value.Identity,
                PlayerId = player.Value.PlayerId,
                Name = player.Value.Name
            });
            ctx.Db.LoggedOutPlayer.Identity.Delete(player.Value.Identity);
        }
        else
        {
            // Brand new player
            ctx.Db.Player.Insert(new Player
            {
                Identity = ctx.Sender,
                Name = ""
            });
        }
    }

    [SpacetimeDB.Reducer(ReducerKind.ClientDisconnected)]
    public static void Disconnect(ReducerContext ctx)
    {
        // 1. Find the player — throw if not found
        var player = ctx.Db.Player.Identity.Find(ctx.Sender)
            ?? throw new Exception("Player not found");

        // 2. Delete all circles belonging to this player and their entities
        foreach (var circle in ctx.Db.Circle.PlayerId.Filter(player.PlayerId))
        {
            var entity = ctx.Db.Entity.EntityId.Find(circle.EntityId)
                ?? throw new Exception("Could not find entity for circle");

            ctx.Db.Entity.EntityId.Delete(entity.EntityId);
            ctx.Db.Circle.EntityId.Delete(circle.EntityId);
        }

        // 3. Move player row to logged-out table
        ctx.Db.LoggedOutPlayer.Insert(new LoggedOutPlayer
        {
            Identity = player.Identity,
            PlayerId = player.PlayerId,
            Name = player.Name
        });
        ctx.Db.Player.Identity.Delete(player.Identity);
    }

    // ─────────────────────────────────────────────
    // SCHEDULED REDUCER
    // ─────────────────────────────────────────────

    [SpacetimeDB.Reducer]
    public static void SpawnFood(ReducerContext ctx, SpawnFoodTimer timer)
    {
        // Don't spawn food if no players are connected
        if (ctx.Db.Player.Count == 0) return;

        // FIX: Config PrimaryKey is now uint Id = 0, not string
        var config = ctx.Db.Config.Id.Find(0)
            ?? throw new Exception("Config not found — was Init called?");

        var rng = ctx.Rng;
        var foodCount = ctx.Db.Food.Count;

        while (foodCount < TARGET_FOOD_COUNT)
        {
            var foodMass = rng.Range(FOOD_MASS_MIN, FOOD_MASS_MAX);
            var foodRadius = MassToRadius(foodMass);
            var x = rng.Range(foodRadius, config.WorldSize - foodRadius);
            var y = rng.Range(foodRadius, config.WorldSize - foodRadius);

            var entity = ctx.Db.Entity.Insert(new Entity
            {
                Position = new DbVector2(x, y),
                Mass = foodMass,
            });

            ctx.Db.Food.Insert(new Food
            {
                EntityId = entity.EntityId,
            });

            foodCount++;
            Log.Info($"Spawned food entity {entity.EntityId}");
        }
    }

    // ─────────────────────────────────────────────
    // SIMPLER REDUCER
    // ─────────────────────────────────────────────
    [Reducer]
    public static void EnterGame(ReducerContext ctx, string name)
	{
		Log.Info($"Creating player with name {name}");
		var player = ctx.Db.Player.Identity.Find(ctx.Sender) ?? throw new Exception("Player not found");
		player.Name = name;
		ctx.Db.Player.Identity.Update(player);
		SpawnPlayerInitialCircle(ctx, player.PlayerId);
	}
    // ─────────────────────────────────────────────
    // REGULAR STATIC HELPER METHOD
    // ─────────────────────────────────────────────
    public static Entity SpawnPlayerInitialCircle(ReducerContext ctx, uint player_id)
	{
		var rng = ctx.Rng;
		var world_size = (ctx.Db.Config.Id.Find(0) ?? throw new Exception("Config not found")).WorldSize;
		var player_start_radius = MassToRadius(START_PLAYER_MASS);
		var x = rng.Range(player_start_radius, world_size - player_start_radius);
		var y = rng.Range(player_start_radius, world_size - player_start_radius);
		return SpawnCircleAt(
			ctx,
			player_id,
			START_PLAYER_MASS,
			new DbVector2(x, y),
			ctx.Timestamp
		);
	}
    public static Entity SpawnCircleAt(ReducerContext ctx, uint player_id, uint mass, DbVector2 position, SpacetimeDB.Timestamp timestamp)
	{
		var entity = ctx.Db.Entity.Insert(new Entity
		{
			Position = position,
			Mass = mass,
		});

		ctx.Db.Circle.Insert(new Circle
		{
			EntityId = entity.EntityId,
			PlayerId = player_id,
			Direction = new DbVector2(0, 1),
			Speed = 0f,
			LastSplitTime = timestamp.MicrosecondsSinceUnixEpoch,
		});
		return entity;
	}

}