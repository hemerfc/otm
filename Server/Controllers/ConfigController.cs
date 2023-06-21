using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection;
using Otm.Server.ContextConfig;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;
using FluentValidation;
using FluentValidation.AspNetCore;
using NLog.Web;

namespace Otm.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        public ILogger<ConfigController> Logger { get; }
        public IConfigService ConfigService { get; }
        public OtmContextManager ContextManager { get; }

        public ConfigController(ILogger<ConfigController> logger, IConfigService configService, OtmContextManager contextManager)
        {
            this.Logger = logger;
            this.ConfigService = configService;
            ContextManager = contextManager;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ConfigFile>), StatusCodes.Status200OK)]
        public IActionResult GetAll()
        {
            var configFiles = ConfigService.GetAll();
            return Ok(configFiles);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OtmContextConfig), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetById(string id)
        {
            var config = ConfigService.Get(id);

            if (config == null)
                return NotFound();

            return Ok(config);
        }

        [HttpPost]
        [ProducesResponseType(typeof(OtmContextConfig), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ModelStateDictionary), StatusCodes.Status400BadRequest)]
        public IActionResult Create(OtmContextConfig config)
        {
            var validation = ConfigService.ValidateCreate(config);

            if (!validation.IsValid)
            {
                validation.AddToModelState(ModelState, null);
                return BadRequest(ModelState);
            }

            return Ok(config);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OtmContextConfig), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(typeof(ModelStateDictionary), StatusCodes.Status400BadRequest)]
        public IActionResult Update(string id, OtmContextConfig config)
        {
            if (!ConfigService.Exists(id))
                return NotFound();

            var validation = ConfigService.ValidateUpdate(id, config);

            if (!validation.IsValid)
            {
                validation.AddToModelState(ModelState, null);
                return BadRequest(ModelState);
            }

            ConfigService.Update(id, config);
            return Ok(config);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Delete(string id)
        {
            if (!ConfigService.Exists(id))
                return NotFound();

            ConfigService.Delete(id);
            return Ok();
        }

    }
}
