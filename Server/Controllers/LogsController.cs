using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Otm.Server.ContextConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Otm.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        public IConfigService ConfigService { get; }
        public ILogsService LogsService { get; }

        public LogsController(IConfigService configService, ILogsService logsService)
        {
            this.ConfigService = configService;
            this.LogsService = logsService;
        }

        // GET api/Logs
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ConfigFile>), StatusCodes.Status200OK)]
        public IActionResult Get()
        {
            var files = LogsService.GetFilesName();
            return Ok(files);
        }
    }
}
