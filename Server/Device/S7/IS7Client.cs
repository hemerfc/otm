namespace Otm.Server.Device.S7
{
    public interface IS7Client
    {
        bool Connected { get; }

        int ConnectTo(string Address, int Rack, int Slot);
        int DBRead(int DBNumber, int Start, int Size, byte[] Buffer);
        int DBWrite(int DBNumber, int Start, int Size, byte[] Buffer);
        int Disconnect();

        string ErrorText(int Error);
    }
}