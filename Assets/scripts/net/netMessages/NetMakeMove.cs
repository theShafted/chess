using Unity.Networking.Transport;
using UnityEngine;

public class NetMakeMove : NetMessage
{
    public int initialFile;
    public int initialRank;

    public int targetFile;
    public int targetRank;

    public int color;

    public NetMakeMove()
    {
        Code = OpCode.MAKE_MOVE;
    }
    public NetMakeMove(DataStreamReader reader)
    {
        Code = OpCode.MAKE_MOVE;
        Deserialize(reader);
    }

    public override void Deserialize(DataStreamReader reader)
    {
        initialFile = reader.ReadInt();
        initialRank = reader.ReadInt();
        
        targetFile = reader.ReadInt();
        targetRank = reader.ReadInt();
        
        color = reader.ReadInt();
    }
    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);

        writer.WriteInt(initialFile);
        writer.WriteInt(initialRank);

        writer.WriteInt(targetFile);
        writer.WriteInt(targetRank);

        writer.WriteInt(color);
    }

    public override void ReceivedOnClient()
    {
        NetUtility.C_MAKE_MOVE?.Invoke(this);
    }
    public override void ReceivedOnServer(NetworkConnection client)
    {
        NetUtility.S_MAKE_MOVE?.Invoke(this, client);
    }
}
