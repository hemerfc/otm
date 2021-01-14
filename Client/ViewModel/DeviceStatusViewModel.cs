using Otm.Shared.Status;
using System;

namespace Otm.Client.ViewModel
{
    public class DeviceStatusViewModel : ViewModelBase
    {
        private string name;
        private bool enabled;
        private bool connected;
        private DateTime lastErrorTime;

        public string Name { get => name; set => SetField(ref name, value); }

        public bool Enabled { get => enabled; set => SetField(ref enabled, value); }

        public bool Connected { get => connected; set => SetField(ref connected, value); }

        public DateTime LastErrorTime { get => lastErrorTime; set => SetField(ref lastErrorTime, value); }

        public void UpdateViewModel(DeviceStatusDto deviceDto)
        {
            Name = deviceDto.Name;
            Enabled = deviceDto.Enabled;
            Connected = deviceDto.Connected;
            LastErrorTime = deviceDto.LastErrorTime;
        }
    }
}
