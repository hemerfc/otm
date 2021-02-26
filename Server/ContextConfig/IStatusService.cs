using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Otm.Shared;
using Otm.Shared.ContextConfig;
using Otm.Shared.Status;

namespace Otm.Server.ContextConfig
{
    public interface IStatusService
    {
        OtmStatusDto Get();
        void SetOtmContextManager(OtmContextManager otmContextManager);
        bool ToggleDebugMessages(string ctxName, string dataPointName);
    }
}