using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using SpacetimeDB;
using SpacetimeDB.Types;

public class GameManager : MonoBehaviour
{
    // Use 127.0.0.1 for clients. 0.0.0.0 is only valid when the *server* listens on all interfaces.
    const string SERVER_URL = "http://127.0.0.1:3000";
    const string MODULE_NAME = "blackholio";
    public static event Action OnConnected;
    public static event Action OnSubscriptionApplied;

    public float borderThickness = 2;
    public Material borderMaterial;

	public static GameManager Instance { get; private set; }
    public static Identity LocalIdentity { get; private set; }
    public static DbConnection Conn { get; private set; }

    public void Start()
    {
       Instance = this;
        Application.targetFrameRate = 60;

        // In order to build a connection to SpacetimeDB we need to register
        // our callbacks and specify a SpacetimeDB server URI and module name.
        var builder = DbConnection.Builder()
            .OnConnect(HandleConnect)
            .OnConnectError(HandleConnectError)
            .OnDisconnect(HandleDisconnect)
            .WithUri(SERVER_URL)
            .WithDatabaseName(MODULE_NAME);

        // If the user has a SpacetimeDB auth token stored in PlayerPrefs (via AuthToken),
        // we can use it to authenticate the connection.
        if (!string.IsNullOrEmpty(AuthToken.Token))
        {
            builder = builder.WithToken(AuthToken.Token);
        }

        // Building the connection will establish a connection to the SpacetimeDB
        // server.
        Conn = builder.Build(); 
    }

    void HandleConnect(DbConnection conn, Identity identity, string token)
    {
        Debug.Log("Connected to SpacetimeDB");
        AuthToken.SaveToken(token);
        LocalIdentity = identity;

        OnConnected?.Invoke();

        //Request all tables
        Conn.SubscriptionBuilder()
            .OnApplied(HandleSubscriptionApplied)
            .SubscribeToAllTables();
    }

    void HandleConnectError(Exception error)
    {
        Debug.LogError("Failed to connect to SpacetimeDB: " + error.Message);
    }

    void HandleDisconnect(DbConnection conn, Exception error)
    {
        Debug.Log("Disconnected from SpacetimeDB");
        if(error != null)
        {
            Debug.LogException(error);
        }
    }

    void HandleSubscriptionApplied(SubscriptionEventContext ctx)
    {
        Debug.Log("Subscription applied");
        OnSubscriptionApplied?.Invoke();

        // Once we have the initial subscription sync'd to the client cache
        // Get the world size from the config table and set up the arena
        var worldSize = Conn.Db.Config.Id.Find(0).WorldSize;
        SetupArena(worldSize);
    }

    public static bool IsConnected()
    {
        return Conn != null && Conn.IsActive;
    }

    public void Disconnect()
    {
        Conn.Disconnect();
        Conn = null;
    }
    private void SetupArena(float worldSize)
    {
        CreateBorderCube(new Vector2(worldSize / 2.0f, worldSize + borderThickness / 2),
            new Vector2(worldSize + borderThickness * 2.0f, borderThickness)); //North
        CreateBorderCube(new Vector2(worldSize / 2.0f, -borderThickness / 2),
            new Vector2(worldSize + borderThickness * 2.0f, borderThickness)); //South
        CreateBorderCube(new Vector2(worldSize + borderThickness / 2, worldSize / 2.0f),
            new Vector2(borderThickness, worldSize + borderThickness * 2.0f)); //East
        CreateBorderCube(new Vector2(-borderThickness / 2, worldSize / 2.0f),
            new Vector2(borderThickness, worldSize + borderThickness * 2.0f)); //West
    }

    private void CreateBorderCube(Vector2 position, Vector2 scale)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Border";
        cube.transform.localScale = new Vector3(scale.x, scale.y, 1);
        cube.transform.position = new Vector3(position.x, position.y, 1);
        cube.GetComponent<MeshRenderer>().material = borderMaterial;
    }
}