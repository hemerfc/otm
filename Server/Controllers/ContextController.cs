using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Otm.Server.ContextConfig;
using Otm.Shared.ContextConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Otm.Server.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ContextController : ControllerBase
    {
        private readonly Dictionary<string, OtmContext> _contexts;
        public ILogger<ConfigController> Logger { get; }
        public IConfigService ConfigService { get; }
        public IContextService ContextService { get; }

        public ContextController(IConfigService configService, IContextService contextService, ILogger<ConfigController> logger)
        {
            this.ConfigService = configService;
            this.ContextService = contextService;
            this.Logger = logger;
            _contexts = new Dictionary<string, OtmContext>();
        }

        // GET api/Context
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ConfigFile>), StatusCodes.Status200OK)]
        public IActionResult Get()
        {
            var configFiles = ConfigService.GetAll();

            return Ok(configFiles);
        }

        // POST api/Context
        [HttpPost]
        public IActionResult Post([FromBody] ContextInput form)
        {
            var result = new ResultApi();         
            try
            {
                ContextService.CreateOrEditContext(form);
                result.result = true;
            }
            catch (Exception e)
            {
                result.result = false;
                result.message = e.Message;
            }

            return Ok(result);
        }

        // POST api/Context/Delete
        [HttpPost]
        [Route("Delete")]
        public IActionResult Delete([FromBody] ContextInput name)
        {
            var result = new ResultApi();
            try
            {
                ContextService.DeleteContext(name);
                result.result = true;
            }
            catch (Exception e)
            {
                result.result = false;
                result.message = e.Message;
            }

            return Ok(result);
        }

        // POST api/Context/ActivedContext
        // Ativa um Contexto especificado pelo parametro form.Name
        [HttpPost]
        [Route("ActivedContext")]
        public IActionResult ActivedContext([FromBody] ContextInput form)
        {
            var result = new ResultApi();
            var configFiles = ConfigService.Get(form.Name);

            if (configFiles.DataPoints == null) {
                result.result = false;
                result.message = "Data point não foi criado!";
            }
            else if(configFiles.Devices == null){
                result.result = false;
                result.message = "Device não foi criado!";
            }
            else if(configFiles.Transactions == null){
                result.result = false;
                result.message = "Transaction não foi criada!";
            }
            else {
                var otmService = new OtmContext(configFiles, Logger);
                _contexts.Add(form.Name, otmService);

                try
                {
                    foreach (var ctx in _contexts.Values)
                    {
                        ctx.Initialize();
                        form.Enabled = ctx.Start();
                        ContextService.CreateOrEditContext(form);
                    }

                    result.result = true;
                }
                catch (Exception e)
                {
                    result.result = false;
                    result.message = e.Message;
                }
            }
            
            return Ok(result);
        }

        // POST api/Context/disableContext
        // Desativa um Contexto especificado pelo parametro form.Name
        [HttpPost]
        [Route("disableContext")]
        public IActionResult disableContext([FromBody] ContextInput form)
        {
            var result = new ResultApi();
            var configFiles = ConfigService.Get(form.Name);
            var otmService = new OtmContext(configFiles, Logger);
            _contexts.Add(form.Name, otmService);

            try
            {
                foreach (var ctx in _contexts.Values)
                {
                    ctx.Initialize();
                    form.Enabled = ctx.Stop();
                    ContextService.CreateOrEditContext(form);
                }

                result.result = true;
            }
            catch (Exception e)
            {
                result.result = false;
                result.message = e.Message;
            }

            return Ok(result);
        }
    }
}
