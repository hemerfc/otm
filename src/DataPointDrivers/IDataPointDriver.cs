
using System;
using Newtonsoft.Json.Linq;
using Otm.Components;

namespace Otm.DataPointDrivers
{
    public interface IDataPointDriver : IBaseComponent
    {
        void Config(JObject config);
        void Exec(Object[] input_params, Object[] output_params);
    }
}