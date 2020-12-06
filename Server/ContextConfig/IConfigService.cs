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
    public interface IConfigService
    {
        IEnumerable<ConfigFile> GetAll();

        OtmContextConfig Get(string id);

        ValidationResult ValidateCreate(OtmContextConfig config);
        ValidationResult ValidateUpdate(string id, OtmContextConfig config);

        void Create(OtmContextConfig config);

        void Update(string oldId, OtmContextConfig config);
        bool Exists(string id);
        void Delete(string id);
    }
}