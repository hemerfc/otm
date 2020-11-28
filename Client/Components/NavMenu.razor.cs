using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Otm.Client.Api;
using Otm.Client.Services;
using Otm.Client.ViewModel;
using System;
using System.Threading.Tasks;

namespace Otm.Client.Components
{
    public partial class NavMenu
    {
        [Inject]
        protected override OtmStatusViewModel ViewModel { get; set; }
        [Inject]
        protected IStatusClient StatusClient { get; set; }
        [Inject]
        public BlazorTimer BlazorTimer { get; set; }

        private bool collapseNavMenu = true;

        private string NavMenuCssClass => collapseNavMenu ? "collapse" : null;

        public void ToggleNavMenu()
        {
            collapseNavMenu = !collapseNavMenu;
        }

        public void OnContextClick(ContextStatusViewModel contextStatus)
        {
            contextStatus.Collapsed = !contextStatus.Collapsed;
        }

        protected override async Task OnInitializedAsync()
        {
            await StatusClient.GetAsync(
                response =>
                {
                    ViewModel.UpdateViewModel(response);
                });
            
            BlazorTimer.OnElapsed += () =>
            {
                Console.WriteLine("Timer Elapsed.");
                
                StatusClient.GetAsync(
                response =>
                {
                    ViewModel.UpdateViewModel(response);
                });

                BlazorTimer.SetTimer(5000);
            };

            BlazorTimer.SetTimer(5000);
        }

    }
}

/*

@code {
    private bool collapseNavMenu = true;

    private string NavMenuCssClass => collapseNavMenu ? "collapse" : null;

    private void ToggleNavMenu()
    {
        collapseNavMenu = !collapseNavMenu;
    }

    private void OnContextClick(ContextState ctx)
    {
        Dispatcher.Dispatch(new UpdateCollapseContext(ctx, !ctx.Collapsed));
    }

    protected override async Task OnInitializedAsync()
    {
        await statusClient.GetAsync(
                okResult =>
                {
                    Dispatcher.Dispatch(new UpdateOtmStatus(okResult, true));
                });
    }
}
*/