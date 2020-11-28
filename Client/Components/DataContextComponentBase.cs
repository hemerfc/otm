using Microsoft.AspNetCore.Components;
using Otm.Client.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Otm.Client.Components
{
    /// <summary>
    /// ComponentBase with DataContext
    /// </summary>
    public abstract class DataContextComponentBase<TModelBase> : ComponentBase where TModelBase : ViewModelBase
    {
        /// <summary>
        /// 
        /// </summary>
        protected abstract TModelBase ViewModel { get; set; }

        /// <inheritdoc />
        protected override async Task OnInitializedAsync()
        {
            if (ViewModel == null) return;

            if (!ViewModel.Initialized) // do once
            {
                await ViewModel.InitializeAsync();
                ViewModel.PropertyChanged += (s, e) => StateHasChanged();
            }

            await base.OnInitializedAsync();
        }
    }
}
