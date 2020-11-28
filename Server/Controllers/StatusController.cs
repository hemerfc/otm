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
using Otm.Shared.Status;

namespace Otm.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        public ILogger<StatusController> Logger { get; }
        public IStatusService StatusService { get; }
        public StatusController(ILogger<StatusController> logger, IStatusService statusService)
        {
            this.Logger = logger;
            this.StatusService = statusService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(OtmStatusDto), StatusCodes.Status200OK)]
        public IActionResult Get()
        {
            var status = StatusService.Get();

            return Ok(status);
        }

    }
}
