using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection;
using Otm.Server.ContextConfig;
using System.Text.Json;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Otm.Shared.ContextConfig;

namespace Otm.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EnviromentController : ControllerBase
    {
        public ILogger<EnviromentController> Logger { get; }
        public IConfigService ConfigService { get; }
        public EnviromentController(ILogger<EnviromentController> logger, IConfigService configService)
        {
            this.Logger = logger;
            this.ConfigService = configService;

        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RootConfig), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetById(string id)
        {
            var config = ConfigService.Get(id);

            if (config == null)
                return NotFound();

            return Ok(config);
        }

        [HttpPost]
        [ProducesResponseType(typeof(RootConfig), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ModelStateDictionary), StatusCodes.Status400BadRequest)]
        public IActionResult Create(RootConfig config)
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
        [ProducesResponseType(typeof(RootConfig), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ModelStateDictionary), StatusCodes.Status400BadRequest)]
        public IActionResult Update(string id, RootConfig config)
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
