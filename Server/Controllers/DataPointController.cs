using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Otm.Server.ContextConfig;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Otm.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataPointController : ControllerBase
    {
        public IConfigService ConfigService { get; }

        public DataPointController(IConfigService configService)
        {
            this.ConfigService = configService;
        }

        // GET api/DataPoint
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ConfigFile>), StatusCodes.Status200OK)]
        public IActionResult Get(string name)
        {
            var configFiles = ConfigService.Get(name);

            return Ok(configFiles.DataPoints);
        }

        // POST api/DataPoint/TestConnectionString
        [HttpPost]
        [Route("TestConnectionString")]
        public string TestConnectionString([FromBody] DataPointInput connection)
        {
            var configFiles = ConfigService.CreateConnection(connection.Config);
            var result = "";
            try
            {
                configFiles.Open();
                result = configFiles.State.ToString();
                configFiles.Close();
            }
            catch (Exception e)
            {
                result = e.Message;
            }

            return result;
        }

        // POST api/DataPoint/GetStoredProcedure
        [HttpPost]
        [Route("GetStoredProcedure")]
        public List<GetStoredProcedure> GetStoredProcedure([FromBody] DataPointInput connection)
        {
            var configFiles = ConfigService.CreateConnection(connection.Config);
            List<GetStoredProcedure> result = new List<GetStoredProcedure>();
            try
            {
                configFiles.Open();
                string commandText = "select * from sys.procedures";
                using (SqlCommand cmd = new SqlCommand(commandText, configFiles))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            GetStoredProcedure newItem = new GetStoredProcedure();

                            newItem.name = reader.GetString(0);
                            newItem.object_id = reader.GetInt32(1).ToString();

                            result.Add(newItem);
                        }
                    }
                    configFiles.Close();
                }
            }
            catch (Exception)
            {
            }

            return result;
        }

        // POST api/DataPoint/GetParamsStoredProcedure
        [HttpPost]
        [Route("GetParamsStoredProcedure")]
        public List<StoredProcedureGetParams> GetParamsStoredProcedure([FromBody] DataPointInput connection)
        {
            var configFiles = ConfigService.CreateConnection(connection.Config);
            List<StoredProcedureGetParams> result = new List<StoredProcedureGetParams>();
            try
            {
                configFiles.Open();
                 string commandText = @"select  
                                       'Parameter_name' = name,  
                                       'Type' = CONVERT(nvarchar, type_name(user_type_id)),  
                                       'Length' = CONVERT(nvarchar, max_length)

                                      from sys.parameters where object_id = " + connection.object_id;

                using (SqlCommand cmd = new SqlCommand(commandText, configFiles))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            StoredProcedureGetParams newItem = new StoredProcedureGetParams();

                            newItem.name = reader.GetString(0);
                            newItem.type = reader.GetString(1);
                            newItem.length = reader.GetString(2);

                            result.Add(newItem);
                        }
                    }
                    configFiles.Close();
                }
            }
            catch (Exception)
            {
            }

            return result;
        }

        // POST api/DataPoint
        [HttpPost]
        public IActionResult Post([FromBody] DataPointConfig form)
        {      
            var result = new ResultApi();
            try
            {
                ConfigService.CreateDatapoint(form);
                result.result = true;
            }
            catch (Exception e)
            {
                result.result = false;
                result.message = e.Message;
            }

            return Ok(result);
        }

        // POST api/DataPoint/Delete
        [HttpPost]
        [Route("Delete")]
        public IActionResult Delete([FromBody] DataPointInput name)
        {
            var result = new ResultApi();
            try {
                ConfigService.DeleteDataPoint(name);
                result.result = true;
            }
            catch(Exception e){
                result.result = false;
                result.message = e.Message;
            }

            return Ok(result);
        }

        // POST api/<DataPointController>
        //[HttpPost]
        //[Route("ExecuteProcedure")]
        //[ProducesResponseType(typeof(IEnumerable<SqlDataReader>), StatusCodes.Status200OK)]
        //public IActionResult ExecuteProcedure([FromBody] DataPointConfig dataPoint)
        //{
        //    var result = ConfigService.executeProcedure(dataPoint);
        //    return (IActionResult)result;
        //}
    }
}
