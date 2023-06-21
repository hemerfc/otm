
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Otm.Server.ContextConfig;
using System.Data.SqlClient;
using Jint;
using Microsoft.Extensions.Logging;

namespace Otm.Server.DataPoint
{
    public class ScriptDataPoint : IDataPoint
    {
        public string Name { get { return Config.Name; } }
        private DataPointConfig Config { get; set; }
        public Engine Engine { get;  }
        public bool DebugMessages { get; set; }
        public string Driver { get; }
        public string Script { get; }
        public string CronExpression { get; }


        public ScriptDataPoint(DataPointConfig config)
        {
            Config = config;
            Engine = new Engine();
            this.DebugMessages = config.DebugMessages;
            this.Driver = config.Driver;
            this.Script = config.Script;
        }

        public DataPointParamConfig GetParamConfig(string name)
        {
            return Config.Params.FirstOrDefault(x => x.Name == name);
        }

        public IDictionary<string, object> Execute(IDictionary<string, object> input)
        {
            foreach (var param in Config.Params)
            {
                if (param.Mode == Modes.FromOTM)
                    Engine.SetValue(param.Name, input[param.Name]);
            }

            Engine.Execute(Config.Script);

            var output = new Dictionary<string, object>();
            foreach (var param in Config.Params)
            {
                if (param.Mode == Modes.ToOTM)
                    output[param.Name] = Convert.ChangeType(Engine.GetValue(param.Name).ToObject(), param.TypeCode);
            }
            return output;
        }

        public bool CheckConnection()
        {
            throw new NotImplementedException();
        }

        public bool CheckFunction()
        {
            throw new NotImplementedException();
        }

        public List<DataPointFunction> GetFunctions()
        {
            throw new NotImplementedException();
        }
    }
}