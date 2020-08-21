using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Otm.Shared.ContextConfig;

namespace Otm.Server.ContextConfig
{
    public interface IStatusService
    {
        RootConfig GetStatus(string configid);
    }
}