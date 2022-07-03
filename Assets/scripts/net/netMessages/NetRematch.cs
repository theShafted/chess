using Unity.Networking.Transport;

public class NetRematch : NetMessage
{

    public int color;
    public byte acceptRematch;

    public NetRematch()
    {
        Code = OpCode.REMATCH;
    }
    public NetRematch(DataStreamReader reader)
    {
        Code = OpCode.REMATCH;
        Deserialize(reader);
    }

    public override void Deserialize(DataStreamReader reader)
    {
        color = reader.ReadInt();
        acceptRematch = reader.ReadByte();
    }
    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
        writer.WriteInt(color);
        writer.WriteByte(acceptRematch);
    }

    public override void ReceivedOnClient()
    {
        NetUtility.C_REMATCH?.Invoke(this);
    }
    public override void ReceivedOnServer(NetworkConnection client)
    {
        NetUtility.S_REMATCH?.Invoke(this, client);
    }
}
