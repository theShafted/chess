using System;
using Unity.Networking.Transport;
using Unity.Collections;
using UnityEngine;

public class Server : MonoBehaviour
{
    // Singleton Implementation
    public static Server Instance {set; get;}

    private void Awake()
    {
        Instance = this;
    }

    public NetworkDriver driver;
    private NativeList<NetworkConnection> connections;

    private bool active = false;
    private const float keepAliveTickRate = 20.0f;
    private float lastKeepAlive;

    public Action connectionDropped;

    // Methods
    public void Init(ushort port)
    {
        driver = NetworkDriver.Create();
        NetworkEndPoint endPoint = NetworkEndPoint.AnyIpv4;
        endPoint.Port = port;

        if (driver.Bind(endPoint) != 0)
        {
            Debug.Log("Unable to bind port on " + endPoint.Port);
            return;
        }
        else driver.Listen();

        connections = new NativeList<NetworkConnection>(2, Allocator.Persistent);
        active = true;
    }

    public void ShutDown()
    {
        if (active)
        {
            driver.Dispose();
            connections.Dispose();
            active = false;
        }
    }
    public void OnDestroy()
    {
        ShutDown();
    }
    
    public void Update()
    {
        if (!active) return;

        KeepAlive();

        driver.ScheduleUpdate().Complete();

        CleanUpConnections();
        AcceptNewConnections();
        UpdateMessage();
    }

    public void KeepAlive()
    {
        if (Time.time - lastKeepAlive > keepAliveTickRate)
        {
            lastKeepAlive = Time.time;
            Broadcast(new NetKeepAlive());
        }
    }

    private void CleanUpConnections()
    {
        for (int connection=0; connection<connections.Length; connection++)
        {
            if (!connections[connection].IsCreated)
            {
                connections.RemoveAtSwapBack(connection);
                --connection;
            }
        }
    }
    private void AcceptNewConnections()
    {
        NetworkConnection connection;

        while ((connection = driver.Accept()) != default(NetworkConnection))
            connections.Add(connection);
    }

    private void UpdateMessage()
    {
        DataStreamReader reader;

        for (int connection=0; connection<connections.Length; connection++)
        {
            NetworkEvent.Type cmd;
            while ((cmd = driver.PopEventForConnection(connections[connection], out reader)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                    NetUtility.OnData(reader, connections[connection], this);
                
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    connections[connection] = default(NetworkConnection);
                    connectionDropped?.Invoke();
                    ShutDown();
                }
            }
        }
    }

    public void SendToClient(NetworkConnection connection, NetMessage msg)
    {
        DataStreamWriter writer;
        driver.BeginSend(connection, out writer);
        msg.Serialize(ref writer);
        driver.EndSend(writer);
    }
    public void Broadcast(NetMessage msg)
    {
        for (int connection=0; connection<connections.Length; connection++)
            if (connections[connection].IsCreated)
            {
                Debug.Log($"Sending {msg.Code} to  {connections[connection].InternalId}");
                SendToClient(connections[connection], msg);
            }
    }
}