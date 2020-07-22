using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentValidation.Results;
using Otm.Shared.ContextConfig;

namespace Otm.Server.ContextConfig
{
    public class StatusService : IStatusService
    {
        public RootConfig GetStatus(string configid)
        {
            throw new NotImplementedException();
        }
    }
}