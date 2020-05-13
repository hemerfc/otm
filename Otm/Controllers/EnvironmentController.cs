using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection;
using Otm.ContextConfig;
using System.Text.Json;
using FluentValidation;
using FluentValidation.AspNetCore;
using Otm.Mvc.ViewModel;
using InfluxDB.Collector;

namespace Otm.Controllers
{
    public class EnviromentController : Controller
    {
        public ILogger<EnviromentController> Logger { get; }
        public IConfigService ConfigService { get; }
        public string Database { get; }
        public Uri Uri { get; }

        public EnviromentController(ILogger<EnviromentController> logger, IConfigService configService)
        {
            this.Logger = logger;
            this.ConfigService = configService;

            this.Database = "telegraf";
            this.Uri = new Uri("http://127.0.0.1:8086");
        }

        public IActionResult Create()
        {
            var config = new RootConfig();

            return View(config);
        }

        [HttpPost]
        public IActionResult Create(RootConfig config)
        {

            var validation = ConfigService.ValidateCreateConfig(config);

            if (!validation.IsValid)
            {
                validation.AddToModelState(ModelState, null);
                return View(config);
            }
            else
            {
                ConfigService.CreateConfig(config);
                return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult Edit(string configid)
        {
            var config = ConfigService.LoadConfig(configid);

            return View("Edit", config);
        }

        [HttpPost]
        public IActionResult Edit(string configid, RootConfig config)
        {
            var validation = ConfigService.ValidateEditConfig(configid, config);

            if (!validation.IsValid)
            {
                validation.AddToModelState(ModelState, null);
                return View(config);
            }
            else
            {
                ConfigService.EditConfig(configid, config);
                return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult Delete(string configid)
        {
            var model = new DeleteViewModel<string>(configid, configid);
            return View(model);
        }

        [HttpPost]
        public IActionResult Delete(DeleteViewModel<string> model)
        {
            if (!model.Confirm ?? false)
            {
                ModelState.AddModelError(nameof(model.Id), "É necessario confirmar a exclusão!");
                return View(model);
            }
            else
            {
                ConfigService.DeleteConfig(model.Id);
                return RedirectToAction("Index", "Home");
            }
        }

    }
}
