using System;

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

    public enum E_PtlMessageType : int
    {
        Picking = 1,
        Message = 2
    }

    public class ReceivePTLViewModel
    {
        public ReceivePTLViewModel(string location, E_DisplayColor displayColor, string displayValue, E_PtlMessageType messageType)
        {
            Location = location;
            DisplayColor = displayColor;
            DisplayValue = displayValue;
            MessageType = messageType;
            DtHoraComando = DateTime.Now;
        }

        public ReceivePTLViewModel(string location, E_DisplayColor displayColor, int displayValue, E_PtlMessageType messageType)
        {
            Location = location;
            DisplayColor = displayColor;
            DisplayValue = displayValue.ToString();
            MessageType = messageType;
            DtHoraComando = DateTime.Now;
        }

        public string Location { get; private set; }
        public E_DisplayColor DisplayColor { get; private set; }
        public string DisplayValue { get; private set; }
        public E_PtlMessageType MessageType { get; private set; }
        public DateTime DtHoraComando { get; private set; }
    }
}
