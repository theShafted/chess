using Unity.Networking.Transport;

public class NetWelcome : NetMessage
{
    public int AssignedColor{set; get;}

    public NetWelcome()
    {
        Code = OpCode.WELCOME;
    }
    public NetWelcome(DataStreamReader reader)
    {
        Code = OpCode.WELCOME;
        Deserialize(reader);
    }

    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
        writer.WriteInt(AssignedColor);
    }
    public override void Deserialize(DataStreamReader reader)
    {
        // The byte is alread read in NetUtility::OnData
        AssignedColor = reader.ReadInt();
    }

    public override void ReceivedOnClient()
    {
        NetUtility.C_WELCOME?.Invoke(this);
    }
    public override void ReceivedOnServer(NetworkConnection client)
    {
        NetUtility.S_WELCOME?.Invoke(this, client);
    }
}
