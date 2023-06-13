using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Otm.Server.ContextConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Otm.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {

        public IConfigService ConfigService { get; }
        public ITransactionService TransactionService { get; }

        public TransactionController(IConfigService configService, ITransactionService transactionService)
        {
            this.ConfigService = configService;

            this.TransactionService = transactionService;
        }

        // GET api/Transaction
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ConfigFile>), StatusCodes.Status200OK)]
        public IActionResult Get(string name)
        {
            var configFiles = ConfigService.Get(name);

            return Ok(configFiles.Transactions);
        }

        // POST api/Transaction
        [HttpPost]
        public IActionResult Post([FromBody] TransactionConfig form)
        {
            var result = new ResultApi();
            try
            {
                TransactionService.CreateOrEditTransaction(form);
                result.result = true;
            }
            catch (Exception e)
            {
                result.result = false;
                result.message = e.Message;
            }

            return Ok(result);
        }

        // POST api/Transaction/Delete
        [HttpPost]
        [Route("Delete")]
        public IActionResult Delete([FromBody] TransactionInput name)
        {
            var result = new ResultApi();
            try
            {
                TransactionService.DeleteTransaction(name);
                result.result = true;
            }
            catch (Exception e)
            {
                result.result = false;
                result.message = e.Message;
            }

            return Ok(result);
        }

        // GET api/GetData?name=
        [HttpPost]
        [Route("GetData")]
        public IActionResult GetData([FromBody] TransactionInput input)
        {
            var configFiles = ConfigService.Get(input.ContextName);

            return Ok(configFiles);
        }
    }
}
