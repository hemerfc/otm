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
        private Dictionary<string, DataPointStatusViewModel> dataPointStatus;

        public string Name { get => name; set => SetField(ref name, value); }

        public bool Enabled { get => enabled; set => SetField(ref enabled, value); }

        public Dictionary<string, DeviceStatusViewModel> DeviceStatus { get => deviceStatus; set => SetField(ref deviceStatus, value); }
        public Dictionary<string, DataPointStatusViewModel> DataPointStatus { get => dataPointStatus; set => SetField(ref dataPointStatus, value); }
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
                        DeviceStatus[deviceDto.Name] = new DeviceStatusViewModel();

                    DeviceStatus[deviceDto.Name].UpdateViewModel(deviceDto);
                }

            if (DataPointStatus == null)
                DataPointStatus = new Dictionary<string, DataPointStatusViewModel>();

            if (ctxDto.DataPointStatus != null)
                foreach (var datapointDto in ctxDto.DataPointStatus.Values)
                {
                    if (!DataPointStatus.ContainsKey(datapointDto.Name))
                        DataPointStatus[datapointDto.Name] = new DataPointStatusViewModel();

                    DataPointStatus[datapointDto.Name].UpdateViewModel(datapointDto);
                }
        }
    }
}
