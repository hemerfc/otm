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
using System.Collections.Generic;

namespace Otm.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LogController : ControllerBase
    {
        public ILogger<EnviromentController> Logger { get; }
        public IConfigService ConfigService { get; }
        public LogController()
        {
        }

        [HttpGet()]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Get()
        {
            var msg = new List<string> { "TESTE 1", "TESTE 2", "TESTE 3" };

            if (msg == null)
                return NotFound();

            return Ok(msg);
        }

    }
}
