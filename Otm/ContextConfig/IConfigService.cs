using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;

namespace Otm.ContextConfig
{
    public interface IConfigService
    {
        IEnumerable<ConfigFile> GetFiles();

        string GetConfigFolder();

        RootConfig LoadConfig(string configPath);

        ValidationResult ValidateCreateConfig(RootConfig config);
        ValidationResult ValidateEditConfig(string configid, RootConfig config);

        void CreateConfig(RootConfig config);

        void EditConfig(string oldConfigId, RootConfig config);
        void DeleteConfig(string id);
    }
}