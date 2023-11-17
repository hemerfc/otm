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

        public PtlBaseClass(Guid id, string location, E_DisplayColor displayColor, float displayValue, E_PTLMasterMessage masterMessage = E_PTLMasterMessage.None)
        {
            Id = id;
            Location = location;
            DisplayColor = displayColor;
            DisplayValue = displayValue.ToString();
            MasterMessage = masterMessage;
            DtHoraComando = DateTime.Now;
        }
        public PtlBaseClass(Guid id, string location, E_DisplayColor displayColor, int displayValue, E_PTLMasterMessage masterMessage = E_PTLMasterMessage.None)
        {
            Id = id;
            Location = location;
            DisplayColor = displayColor;
            DisplayValue = displayValue.ToString();
            MasterMessage = masterMessage;
            DtHoraComando = DateTime.Now;
        }
        public PtlBaseClass(Guid id, string location, E_DisplayColor displayColor, string displayValue, E_PTLMasterMessage masterMessage = E_PTLMasterMessage.None)
        {
            Id = id;
            Location = location;
            DisplayColor = displayColor;
            DisplayValue = displayValue;
            MasterMessage = masterMessage;
            DtHoraComando = DateTime.Now;
        }

        public PtlBaseClass(Guid id, string location, string displayColor, string displayValue, E_PTLMasterMessage masterMessage = E_PTLMasterMessage.None)
        {
            Id = id;
            Location = location;
            DisplayColorInt = displayColor;
            DisplayValue = displayValue;
            MasterMessage = masterMessage;
            DtHoraComando = DateTime.Now;
        }

        public Guid Id { get; private set;}
        public string Location { get; private set; }
        public E_DisplayColor DisplayColor { get; private set; }
        public string DisplayColorInt { get; private set; }
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
            var filteredDisplayValue = DisplayValue.PadLeft(6);

            //var buffer = new List<byte>();
            //foreach (var x in filteredDisplayValue)
            //{
            //    var (found, result) = Dict.GetValue(x);
            //
            //    if (found)
            //        buffer.Add(result);
            //}
            //return buffer.ToArray();

            var buffer = new byte[filteredDisplayValue.Length];
            Str2Bin(filteredDisplayValue, ref buffer, 0);
            return buffer;
        }

        public static int Asc(string S)
        {
            int N = Convert.ToInt32(S[0]);
            return N;
        }

        static public int Str2Bin(string strdata, ref byte[] bufbin, int start)
        {
            int returnValue;
            //// change string to byte array
            int i;
            int strcnt;
            int ndx;
            int data;
            int val1, val2;

            strcnt = strdata.Length;
            ndx = start;
            for (i = 1; i <= strcnt; i++)
            {
                data = Asc(strdata.Substring(i - 1, 1));
                val1 = (data % 256) & 0xFF;
                bufbin[ndx] = (byte)val1;
                ndx++;
                if (data >= 256)
                {
                    val2 = (int)(data / 256) & 0xFF;
                    bufbin[ndx] = (byte)val2;
                    ndx++;
                }
            }

            returnValue = ndx;
            return returnValue;
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
            catch (Exception)
            {
                return 0;
            }
            
        }

        internal int GetDisplayIdToInt()
        {
            try
            {
                return Int32.Parse((Location.Split(":")[1]));
            }
            catch (Exception)
            {
                return 0;
            }

        }

        /// <summary>
        /// Get DisplayId based on Location split
        /// </summary>
        /// <returns></returns>
        internal byte GetDisplayIdBroker()
        {
            try
            {
                if (byte.TryParse(Location, out var result))
                    return result;
                else
                    return 0;
            }
            catch (Exception)
            {
                return 0;
            }

        }
    }
}
