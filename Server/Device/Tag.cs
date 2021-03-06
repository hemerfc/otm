using System;

namespace Otm.Server.DeviceDrivers
{
    public class Tag
    {
        private string _name { get; }
        private object _value;

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public object Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                TagChanged?.Invoke(_name, _value);
            }
        }

        public delegate void TagChangedEventHandler(string tagName, object tagValue);
        public event TagChangedEventHandler TagChanged;

        public Tag(string name, object value)
        {
            _name = name;
            _value = value;
        }
    }
}