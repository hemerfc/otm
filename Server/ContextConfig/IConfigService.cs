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

        RootConfig Get(string id);

        ValidationResult ValidateCreate(RootConfig config);
        ValidationResult ValidateUpdate(string id, RootConfig config);

        void Create(RootConfig config);

        void Update(string oldId, RootConfig config);
        bool Exists(string id);
        void Delete(string id);
    }
}