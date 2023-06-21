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
using System.Collections.Generic;
using Otm.Server;

namespace Otm.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogReaderController : ControllerBase
    {
        public ILogger<LogReaderController> Logger { get; }
        public IConfigService ConfigService { get; }
        public LogReaderController()
        {
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<LogMessage>), StatusCodes.Status200OK)]
        public IActionResult GetMessages(string origin, string level = null)
        {
            var msgs = Enumerable.Range(0, 10)
                .Select(x => new LogMessage(Guid.NewGuid(), DateTime.Now, "Debug", origin, $"Message {x:00000}."));

            return Ok(msgs);
        }

    }
}
