using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentValidation.Results;
using Otm.Server.Device;
using Otm.Shared;
using Otm.Shared.ContextConfig;
using Otm.Shared.Status;

namespace Otm.Server.ContextConfig
{
    public class StatusService : IStatusService
    {
        private OtmContextManager OtmContextManager;

        public void SetOtmContextManager(OtmContextManager otmContextManager)
        {
            OtmContextManager = otmContextManager;
        }

        public OtmStatusDto Get()
        {
            var otmStatus = new OtmStatusDto
            {
                OtmContexts = new Dictionary<string, ContextStatusDto>()
            };
            foreach (var ctx in this.OtmContextManager.Contexts)
            {
                otmStatus.OtmContexts.Add(ctx.Key, GetOtmContextStatus(ctx.Value));
            }
            return otmStatus;
        }

        private static ContextStatusDto GetOtmContextStatus(OtmContext ctx)
        {
            var ctxStatus = new ContextStatusDto
            {
                Name = ctx.Config.Name,
                Enabled = ctx.Config.Enabled,
                DeviceStatus = new Dictionary<string, DeviceStatusDto>()
            };

            if (ctx.Devices != null)
                foreach (var device in ctx.Devices.Values)
                {
                    var deviceStatusDto = new DeviceStatusDto
                    {
                        Name = device.Name,
                        Enabled = device.Enabled,
                        Connected = device.Connected,
                        LastErrorTime = device.LastErrorTime,
                        TagValues = device.TagValues
                    };

                    ctxStatus.DeviceStatus[device.Name] = deviceStatusDto;
                }

            return ctxStatus;
        }

    }
}