using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Otm.Server.Device.S7;
using Otm.Server.ContextConfig;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Otm.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class deviceController : ControllerBase
    {
        public IConfigService ConfigService { get; }
        public IDeviceService DeviceService { get; }

        public deviceController(IConfigService configService, IDeviceService deviceService)
        {
            this.ConfigService = configService;
            this.DeviceService = deviceService;
        }

        // GET api/Device
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ConfigFile>), StatusCodes.Status200OK)]
        public IActionResult Get(string name)
        {
            var configFiles = ConfigService.Get(name);

            return Ok(configFiles.Devices);
        }

        // POST api/Device
        [HttpPost]
        public IActionResult Post([FromBody] DeviceConfig form)
        {          
            var result = new ResultApi();
            try
            {
                DeviceService.CreateOrEditDevice(form);
                result.result = true;
            }
            catch (Exception e)
            {
                result.result = false;
                result.message = e.Message;
            }

            return Ok(result);
        }

        // POST api/Device/Delete
        [HttpPost]
        [Route("Delete")]
        public IActionResult Delete([FromBody] DeviceInput name)
        {
            var result = new ResultApi();
            try
            {
                DeviceService.DeleteDevice(name);
                result.result = true;
            }
            catch (Exception e)
            {
                result.result = false;
                result.message = e.Message;
            }

            return Ok(result);
        }

        // POST api/Device/TestConnectionS7
        [HttpPost]
        [Route("TestConnectionS7")]
        public IActionResult TestConnectionS7([FromBody] DeviceInput input)
        {
            var result = new ResultApi();
            try
            {
                var client = new S7Client() { PduSizeRequested = 960 };
                client.ConnectTo(input.host, input.slot, input.rack);
                if (client.Connected)
                {
                    client.Disconnect();
                    result.result = true;
                }
                else {
                    result.result = false;
                }
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
