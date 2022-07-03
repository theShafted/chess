using System;
using Unity.Networking.Transport;
using Unity.Collections;
using UnityEngine;

public class Client : MonoBehaviour
{
   // Singleton implementation
   public static Client Instance {set; get;}

   private void Awake()
   {
        Instance = this;
   }

   public NetworkDriver driver;
   private NetworkConnection connection;

   private bool active = false;
   public Action connectionDropped;

   // Methods
    public void Init(string ip, ushort port)
    {
        driver = NetworkDriver.Create();
        NetworkEndPoint endPoint = NetworkEndPoint.Parse(ip, port);

        connection = driver.Connect(endPoint);

        Debug.Log("Attempting to connect to server on " + endPoint.Address);

        active = true;

        RegisterToEvent();
    }
    public void ShutDown()
    {
        if (active)
        {
            UnRegisterToEvent();
            driver.Dispose();
            connection = default(NetworkConnection);
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

        driver.ScheduleUpdate().Complete();

        CheckAlive();
        UpdateMessage();
    }

    private void CheckAlive()
    {
        if (!connection.IsCreated && active)
        {
            Debug.Log("Something went wrong, lost connection to server");
            connectionDropped?.Invoke();
            ShutDown();
        }
    }
    private void UpdateMessage()
    {
        DataStreamReader reader;
        NetworkEvent.Type cmd;
        while ((cmd = connection.PopEvent(driver, out reader)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                SendToServer(new NetWelcome());
                Debug.Log("Connected to server");
            }
            
            else if (cmd == NetworkEvent.Type.Data)
            {
                NetUtility.OnData(reader, default(NetworkConnection));
            }

            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Disconnected from server");
                connection = default(NetworkConnection);
                connectionDropped?.Invoke();
                ShutDown();
            }
        }
    }
    public void SendToServer(NetMessage msg)
    {
        DataStreamWriter writer;
        driver.BeginSend(connection, out writer);
        msg.Serialize(ref writer);
        driver.EndSend(writer);
    }

    // Event Parsing
    private void RegisterToEvent()
    {
        NetUtility.C_KEEP_ALIVE += OnKeepAlive;
    }
    private void UnRegisterToEvent()
    {
        NetUtility.C_KEEP_ALIVE -= OnKeepAlive;
    }
    private void OnKeepAlive(NetMessage msg)
    {
        SendToServer(msg);
    }
}
