using System;
using Xunit;
using Moq;
using Otm.Shared.ContextConfig;
using Microsoft.Extensions.Logging;
using Otm.Server.DataPoint;
using System.Linq;

namespace Otm.Test.DataPoint
{
    public class DataPointFactoriesTests
    {
        [Fact]
        public void InvalidDataPointName()
        {
            // prepare
            var dpConfig = new DataPointConfig[]{
                new DataPointConfig {
                    Name = "",
                    Driver = "",
                    Config = "",
                    Params = null
                }
            };

            var loggerMock = new Mock<ILogger>();
            var ex = Record.Exception(() => DataPointFactory.CreateDataPoints(dpConfig, loggerMock.Object));

            Assert.Equal("Name", ex?.Data["field"]);
        }

        [Fact]
        public void InvalidDriverName()
        {
            // prepare
            var dpConfig = new DataPointConfig[]{
                new DataPointConfig {
                    Name = "dp01",
                    Driver = "",
                    Config = "",
                    Params = (new DataPointParamConfig[] {
                        new DataPointParamConfig {
                            Name = "@p1",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.FromOTM
                        }
                    }).ToList()
                }
            };

            var loggerMock = new Mock<ILogger>();
            var ex = Record.Exception(() => DataPointFactory.CreateDataPoints(dpConfig, loggerMock.Object));

            Assert.Equal("Driver", ex?.Data["field"]);
        }

        [Fact]
        public void InvalidNullParameterList()
        {
            // prepare
            var dpConfig = new DataPointConfig[]{
                new DataPointConfig {
                    Name = "dp01",
                    Driver = "pg",
                    Config = "",
                    Params = null
                }
            };

            var loggerMock = new Mock<ILogger>();
            var ex = Record.Exception(() => DataPointFactory.CreateDataPoints(dpConfig, loggerMock.Object));

            Assert.Equal("Params", ex?.Data["field"]);
        }

        [Fact]
        public void InvalidStringParamWithoutLenght()
        {
            // prepare
            var dpConfig = new DataPointConfig[]{
                new DataPointConfig {
                    Name = "dp01",
                    Driver = "mssql",
                    Config = "",
                    Params = (new DataPointParamConfig[] {
                        new DataPointParamConfig {
                            Name = "@p1",
                            TypeCode = TypeCode.String,
                            Mode = Modes.FromOTM
                        }
                    }).ToList()
                }
            };

            var loggerMock = new Mock<ILogger>();
            var ex = Record.Exception(() => DataPointFactory.CreateDataPoints(dpConfig, loggerMock.Object));

            Assert.Equal("Length", ex?.Data["field"]);
        }
        [Fact]
        public void CreateOdbcDataPoint()
        {
            // prepare
            var dpConfig = new DataPointConfig[]{
                new DataPointConfig {
                    Name = "dp01",
                    Driver = "pg",
                    Config = "dsn=DB01",
                    Params = (new DataPointParamConfig[] {
                        new DataPointParamConfig {
                            Name = "@p1",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.FromOTM
                        }
                    }).ToList()
                },
                new DataPointConfig {
                    Name = "dp02",
                    Driver = "pg",
                    Config = "dsn=DB02",
                    Params = (new DataPointParamConfig[] {
                        new DataPointParamConfig {
                            Name = "@p1",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.FromOTM
                        }
                    }).ToList()
                }
            };

            var loggerMock = new Mock<ILogger>();
            var datapoints = DataPointFactory.CreateDataPoints(dpConfig, loggerMock.Object);

            Assert.Equal(2, datapoints.Count);

            Assert.IsType<PgDataPoint>(datapoints["dp01"]);

            Assert.IsType<PgDataPoint>(datapoints["dp02"]);
        }
    }
}
