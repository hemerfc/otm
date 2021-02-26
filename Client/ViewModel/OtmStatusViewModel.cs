using Otm.Shared.Status;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Otm.Client.ViewModel
{
    public class OtmStatusViewModel : ViewModelBase
    {
        private bool loading;
        private Dictionary<string, ContextStatusViewModel> contextsStatus;

        public bool Loading { get => loading; set => SetField(ref loading, value); }
        public Dictionary<string, ContextStatusViewModel> ContextsStatus { get => contextsStatus; set => SetField(ref contextsStatus, value); }

        public void UpdateViewModel(OtmStatusDto otmStatusDto)
        {
            if (ContextsStatus == null)
            {
                ContextsStatus = new Dictionary<string, ContextStatusViewModel>();
            }

            foreach (var ctxDto in otmStatusDto.OtmContexts.Values)
            {
                if (!ContextsStatus.ContainsKey(ctxDto.Name))
                    ContextsStatus[ctxDto.Name] = new ContextStatusViewModel();

                ContextsStatus[ctxDto.Name].UpdateViewModel(ctxDto);
            }
        }
    }
}
