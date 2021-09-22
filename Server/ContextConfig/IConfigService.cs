using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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

        SqlConnection CreateConnection(string connection);

        void CreateDatapoint(DataPointConfig dataPoint);

        void DeleteDataPoint(DataPointInput name);

        //SqlDataReader executeProcedure(DataPointConfig dataPoint);
    }
}