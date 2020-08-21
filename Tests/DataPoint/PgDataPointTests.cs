using System;
using System.Linq;
using Xunit;
using System.Collections.Generic;
using Moq;
using Otm.Shared.ContextConfig;
using Otm.Server.DataPoint;
using Microsoft.Extensions.Logging;

namespace Otm.Test.DataPoint
{
    public class PgDataPointTests
    {
        [Fact]
        public void Execute_DataPoint_sp_test01()
        {
            // prepare
            var dpConfig = new DataPointConfig[]{
                new DataPointConfig {
                    Name = "sp_test01",
                    Driver = "pg",
                    Config = "Host=localhost;Database=otm;Username=otm;Password=otm",
                    Params = (new DataPointParamConfig[] {
                        new DataPointParamConfig {
                            Name = "p1",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.FromOTM
                        },
                        new DataPointParamConfig {
                            Name = "p2",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.ToOTM
                        }
                    }).ToList()
                }
            };

            var inParams = new Dictionary<string, object>();
            inParams["p1"] = 2;
            inParams["p2"] = 0;

            var loggerMock = new Mock<ILogger>();
            var dpSpTest01 = DataPointFactory.CreateDataPoints(dpConfig, loggerMock.Object)["sp_test01"];

            var outParams = dpSpTest01.Execute(inParams);

            Assert.Equal(4, outParams["p2"]);
        }

        [Fact]
        public void Execute_DataPoint_sp_test02()
        {
            // prepare
            var dpConfig = new DataPointConfig[]{
                new DataPointConfig {
                    Name = "sp_test02",
                    Driver = "pg",
                    Config = "Host=localhost;Database=otm;Username=otm;Password=otm",
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
            var dpSpTest01 = DataPointFactory.CreateDataPoints(dpConfig, loggerMock.Object)["sp_test02"];

            var outParams = dpSpTest01.Execute(inParams);

            Assert.Equal(20, outParams["@p3"]);
        }

        [Fact]
        public void Execute_DataPoint_sp_test_ptl01()
        {
            // prepare
            var dpConfig = new DataPointConfig[]{
                new DataPointConfig {
                    Name = "sp_test_ptl01",
                    Driver = "pg",
                    Config = "Host=localhost;Database=otm;Username=otm;Password=otm",
                    Params = (new DataPointParamConfig[] {
                        new DataPointParamConfig {
                            Name = "@p1",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.FromOTM
                        },
                        new DataPointParamConfig {
                            Name = "@p2",
                            TypeCode = TypeCode.String,
                            Mode = Modes.FromOTM,
                            Length = 100
                        },
                        new DataPointParamConfig {
                            Name = "@p3",
                            TypeCode = TypeCode.String,
                            Mode = Modes.ToOTM,
                            Length = 100
                        }
                    }).ToList()
                }
            };

            var inParams = new Dictionary<string, object>();
            inParams["@p1"] = 1;
            inParams["@p2"] = "teste";
            inParams["@p3"] = "";

            var loggerMock = new Mock<ILogger>();
            var dpSpTest01 = DataPointFactory.CreateDataPoints(dpConfig, loggerMock.Object)["sp_test_ptl01"];

            var outParams = dpSpTest01.Execute(inParams);

            Assert.Equal("teste1", outParams["@p3"]);
        }
    }
}
