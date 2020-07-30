namespace Otm.Server.Device.Ptl
{
    public enum E_DisplayColor : byte
    {
        Vermelho = 0x00,
        Verde = 0x01,
        Laranja = 0x02,
        Azul = 0x03,
        Rosa = 0x04,
        Aqua = 0x05,
        Off = 0x06
    }
    public static class DisplayColorExtensions
    {
        public static E_DisplayColor GetDefault(this E_DisplayColor E_DisplayColor)
        {
            return E_DisplayColor.Vermelho;
        }
    }
}
