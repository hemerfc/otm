using Otm.Shared.Status;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Otm.Client.ViewModel
{
    public class ContextStatusViewModel : ViewModelBase
    {
        #region Dto
        private string name;
        private bool enabled;
        private Dictionary<string, DeviceStatusViewModel> deviceStatus;

        public string Name { get => name; set => SetField(ref name, value); }

        public bool Enabled { get => enabled; set => SetField(ref enabled, value); }

        public Dictionary<string, DeviceStatusViewModel> DeviceStatus { get => deviceStatus; set => SetField(ref deviceStatus, value); }
        #endregion

        private bool collapsed = true;

        public bool Collapsed { get => collapsed; set => SetField(ref collapsed, value); }

        public void UpdateViewModel(ContextStatusDto ctxDto)
        {
            Name = ctxDto.Name;
            Enabled = ctxDto.Enabled;

            if (DeviceStatus == null)
                DeviceStatus = new Dictionary<string, DeviceStatusViewModel>();

            if (ctxDto.DeviceStatus != null)
                foreach (var deviceDto in ctxDto.DeviceStatus.Values)
                {
                    if (!DeviceStatus.ContainsKey(deviceDto.Name))
                        DeviceStatus[ctxDto.Name] = new DeviceStatusViewModel();

                    DeviceStatus[ctxDto.Name].UpdateViewModel(deviceDto);
                }
        }
    }
}
