using Otm.Shared.Status;
using System;

namespace Otm.Client.ViewModel
{
    public class DataPointStatusViewModel : ViewModelBase
    {
        private string name;
        private bool debugMessages;
        private bool connected;
        private DateTime lastErrorTime;
        private string script;
        private string driver;

        public string Name { get => name; set => SetField(ref name, value); }

        public bool DebugMessages { get => debugMessages; set => SetField(ref debugMessages, value); }

        public string Script { get => script; set => SetField(ref script, value); }

        public string Driver { get => driver; set => SetField(ref driver, value); }

        public void UpdateViewModel(DataPointStatusDto dataPointDto)
        {
            Name = dataPointDto.Name;
            DebugMessages = dataPointDto.DebugMessages;
            Script = dataPointDto.Script;
            Driver = dataPointDto.Driver;
        }
    }
}
