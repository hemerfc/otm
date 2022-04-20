using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Otm.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PickToLightController : Controller
    {
        // GET api/Context
        [HttpGet]
        public IActionResult Get()
        {
            //Uses a remote endpoint to establish a socket connection.
            UdpClient udpClient = new UdpClient();
            IPAddress ipAddress = Dns.Resolve("127.0.0.1").AddressList[0]; ;
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 11004);
            try
            {
                udpClient.Connect(ipEndPoint);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return Ok();
        }
    }
}
