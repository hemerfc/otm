using System.Collections.Generic;

namespace Otm.Server.Device.Ptl
{
    public class PtlCharDict
    {
        public Dictionary<char, byte> Dictionary { get; private set; }

        public PtlCharDict()
        {
            Dictionary = new Dictionary<char, byte>(){
                {'0', 0x30},
                {'1', 0x31},
                {'2', 0x32},
                {'3', 0x33},
                {'4', 0x34},
                {'5', 0x35},
                {'6', 0x36},
                {'7', 0x37},
                {'8', 0x38},
                {'9', 0x39},
                {'A', 0x41},
                {'b', 0x62},
                {'C', 0x43},
                {'d', 0x64},
                {'E', 0x45},
                {'F', 0x46},
                {'c', 0x63},
                {'H', 0x48},
                {'L', 0x4C},
                {' ', 0x20},
                {'n', 0x6E},
                {'o', 0x6F},
                {'h', 0x68},
                {'r', 0x72},
                {'u', 0x75},
                {'i', 0x69},
                {'y', 0x79},
                {'t', 0x74},
                {'q', 0x71},
                {'G', 0x47},
                {'S', 0x53},
                {'l', 0x6C},
                {'O', 0x4F},
                {'[', 0x5B},
                {']', 0x5D},
                {'P', 0x50},
                {'U', 0x55},
                {'-', 0x2D},
                //MultiSegmento
                {'D', 0x44},
                {'K', 0x4B},
                {'I', 0x49},
                {'M', 0x4D},
                {'N', 0x4E},
                {'T', 0x54},
                {'V', 0x56},
                {'W', 0x57}
            };            
        }

        public (bool found, byte result) GetValue(char element)
        {
            var found = Dictionary.TryGetValue(element, out byte result);

            return (found, result);
        }
    }
}
