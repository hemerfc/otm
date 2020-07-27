using System;
using System.Linq;
using Xunit;
using System.Collections.Generic;
using Moq;
using Otm.Shared.ContextConfig;
using Microsoft.Extensions.Logging;
using Otm.Server.DataPoint;

namespace Otm.Test.DataPoint
{
    public class MsSqlDataPointTests
    {
        [Fact]
        public void Execute_DataPoint_sp_test01()
        {
            // prepare
            var dpConfig = new DataPointConfig[]{
                new DataPointConfig {
                    Name = "[dbo].[sp_test01]",
                    Driver = "mssql",
                    Config = "Server=localhost;Database=QuickFlowDb;User Id=sa;Password=Aguia3220;",
                    Params = (new DataPointParamConfig[] {
                        new DataPointParamConfig {
                            Name = "@p1",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.FromOTM
                        },
                        new DataPointParamConfig {
                            Name = "@p2",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.ToOTM
                        }
                    }).ToList()
                }
            };

            var inParams = new Dictionary<string, object>();
            inParams["@p1"] = 2;
            inParams["@p2"] = 0;

            var loggerMock = new Mock<ILogger>();
            var dpSpTest01 = DataPointFactory.CreateDataPoints(dpConfig, loggerMock.Object)["[dbo].[sp_test01]"];

            var outParams = dpSpTest01.Execute(inParams);

            Assert.Equal(4, outParams["@p2"]);
        }

        [Fact]
        public void Execute_DataPoint_sp_test02()
        {
            // prepare
            var dpConfig = new DataPointConfig[]{
                new DataPointConfig {
                    Name = "[dbo].[sp_test02]",
                    Driver = "mssql",
                    Config = "Server=localhost;Database=QuickFlowDb;User Id=sa;Password=Aguia3220;",
                    Params = (new DataPointParamConfig[] {
                        new DataPointParamConfig {
                            Name = "@p1",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.FromOTM
                        },
                        new DataPointParamConfig {
                            Name = "@p2",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.Static,
                            Value = 2
                        },
                        new DataPointParamConfig {
                            Name = "@p3",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.ToOTM
                        }
                    }).ToList()
                }
            };

            var inParams = new Dictionary<string, object>();
            inParams["@p1"] = 10;
            inParams["@p2"] = 2;
            inParams["@p3"] = 0;

            var loggerMock = new Mock<ILogger>();
            var dpSpTest01 = DataPointFactory.CreateDataPoints(dpConfig, loggerMock.Object)["[dbo].[sp_test02]"];

            var outParams = dpSpTest01.Execute(inParams);

            Assert.Equal(20, outParams["@p3"]);
        }
    }
}
