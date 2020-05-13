
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Otm.ContextConfig;
using System.IO;

namespace Otm.Mvc
{
    public class MainMenuViewComponent : ViewComponent
    {
        //private readonly ToDoContext db;
        public ILogger<MainMenuViewComponent> Logger { get; }
        public IConfigService ConfigService { get; }

        public MainMenuViewComponent(ILogger<MainMenuViewComponent> logger, IConfigService configService)//ToDoContext context)
        {
            this.Logger = logger;
            this.ConfigService = configService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            //var items = await GetItemsAsync(maxPriority, isDone);
            Logger.LogWarning("MainMenuViewComponent.InvokeAsync");

            var files = ConfigService.GetFiles();

            return View(files);
        }
    }
}