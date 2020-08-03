using System;
using System.Collections.Generic;

namespace Otm.Server.Device.Ptl
{
    /// <summary>
    /// Base PickToLight Class with operations
    /// </summary>
    public class PtlBaseClass
    {
        private PtlCharDict Dict { get; set; } = new PtlCharDict();

        public PtlBaseClass(string location, E_DisplayColor displayColor, float displayValue, E_PTLMasterMessage masterMessage = E_PTLMasterMessage.None)
        {
            Location = location;
            DisplayColor = displayColor;
            DisplayValue = displayValue.ToString();
            MasterMessage = masterMessage;
            DtHoraComando = DateTime.Now;
        }
        public PtlBaseClass(string location, E_DisplayColor displayColor, int displayValue, E_PTLMasterMessage masterMessage = E_PTLMasterMessage.None)
        {
            Location = location;
            DisplayColor = displayColor;
            DisplayValue = displayValue.ToString();
            MasterMessage = masterMessage;
            DtHoraComando = DateTime.Now;
        }
        public PtlBaseClass(string location, E_DisplayColor displayColor, string displayValue, E_PTLMasterMessage masterMessage = E_PTLMasterMessage.None)
        {
            Location = location;
            DisplayColor = displayColor;
            DisplayValue = displayValue;
            MasterMessage = masterMessage;
            DtHoraComando = DateTime.Now;
        }

        public string Location { get; private set; }
        public E_DisplayColor DisplayColor { get; private set; }
        public string DisplayValue { get; private set; }
        public E_PTLMasterMessage MasterMessage { get; private set; } = E_PTLMasterMessage.None;
        public DateTime DtHoraComando { get; private set; }


        public E_PtlMessageType MessageType => MasterMessage.MessageType();
        public bool IsMasterMessage => (MessageType == E_PtlMessageType.MasterMessage);
        public bool IsBlinking => (MasterMessage == E_PTLMasterMessage.ConfirmValue);

        /// <summary>
        /// Set DisplayColor value
        /// </summary>
        /// <param name="displayColor"></param>
        internal void SetColor(E_DisplayColor displayColor)
        {
            DisplayColor = displayColor;
        }

        /// <summary>
        /// Set DisplayValue value
        /// </summary>
        /// <param name="displayValue"></param>
        internal void SetDisplayValue(string displayValue)
        {
            DisplayValue = displayValue;
        }

        /// <summary>
        /// Convert the string DisplayValue as byte array, based on conversion dictionary.
        /// </summary>
        /// <returns></returns>
        internal byte[] GetDisplayValueAsByteArray()
        {
            var buffer = new List<byte>();

            var filteredDisplayValue = DisplayValue.PadLeft(6);

            foreach (var x in filteredDisplayValue)
            {
                var (found, result) = Dict.GetValue(x);

                if (found)
                    buffer.Add(result);
            }

            return buffer.ToArray();
        }

        /// <summary>
        /// Get DisplayId based on Location split
        /// </summary>
        /// <returns></returns>
        internal byte GetDisplayId()
        {
            try
            {
                if (byte.TryParse(Location.Split(":")[1], out var result))
                    return result;
                else
                    return 0;
            }
            catch (Exception ex)
            {
                return 0;
            }
            
        }
    }
}
