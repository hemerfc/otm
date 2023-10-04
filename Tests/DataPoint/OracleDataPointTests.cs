using System;
using System.Linq;
using Xunit;
using System.Collections.Generic;
using Moq;
using Otm.Server.ContextConfig;
using NLog;
using Otm.Server.DataPoint;
using Nest;

namespace Otm.Test.DataPoint
{
    public class OracleDataPointTests
    {
        private static string ConnStr = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=FREEPDB1)));User Id=system;Password=senha;";

        [Fact]
        public void Execute_DataPoint_sp_test01()
        {
            /* PLSQL
             CREATE OR REPLACE PROCEDURE ANONYMOUS.SP_TEST01 
               (p1 number,
               p2 in out number) 
               IS
               BEGIN
               p2 := p1*2;
               END SP_TEST01;
               
             */
            
            // prepare
            var dpConfig = new DataPointConfig[]{
                new DataPointConfig {
                    Name = "ANONYMOUS.SP_TEST01",
                    Driver = "oracle",
                    Config = ConnStr,
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
            inParams["@p1"] = 25;
            inParams["@p2"] = 0;

            var loggerMock = new Mock<ILogger>();
            var dpSpTest01 = DataPointFactory.CreateDataPoints(dpConfig, loggerMock.Object)["ANONYMOUS.SP_TEST01"];

            var outParams = dpSpTest01.Execute(inParams);

            Assert.Equal(50, outParams["@p2"]);
        }

        [Fact]
        public void Execute_DataPoint_sp_test02()
        {
            /* PLSQL
             CREATE OR REPLACE PROCEDURE ANONYMOUS.SP_TEST02
               (p1 number,p2 number,
               p3 in out number) 
               IS
               BEGIN
               p3 := p1*p2;
               END SP_TEST02;               
             */
            
            // prepare
            var dpConfig = new DataPointConfig[]{
                new DataPointConfig {
                    Name = "ANONYMOUS.SP_TEST02",
                    Driver = "oracle",
                    Config = ConnStr,
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
            inParams["@p1"] = 15;
            inParams["@p2"] = 2;
            inParams["@p3"] = 0;

            var loggerMock = new Mock<ILogger>();
            var dpSpTest01 = DataPointFactory.CreateDataPoints(dpConfig, loggerMock.Object)["ANONYMOUS.SP_TEST02"];

            var outParams = dpSpTest01.Execute(inParams);

            Assert.Equal(30, outParams["@p3"]);
        }

        [Fact]
        public void Execute_DataPoint_sp_test_ptl01()
        {
            /* PLSQL
              CREATE OR REPLACE PROCEDURE SP_TEST_STRING
               (p1 INT16,
               p2 VARCHAR2,
               p3 in out VARCHAR2) 
               IS
               BEGIN
               
               p3 := TRIM(p2) || TO_CHAR(p1);
               
               END SP_TEST_STRING;
             */
            // prepare
            var dpConfig = new DataPointConfig[]{
                new DataPointConfig {
                    Name = "ANONYMOUS.SP_TEST_STRING",
                    Driver = "oracle",
                    Config = ConnStr,
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
                            Length = 100,
                        },
                        new DataPointParamConfig {
                            Name = "@p3",
                            TypeCode = TypeCode.String,
                            Mode = Modes.ToOTM,
                            Length = 100,
                        }
                    }).ToList()
                }
            };

            var inParams = new Dictionary<string, object>();
            inParams["@p1"] = 12;
            inParams["@p2"] = "teste";
            inParams["@p3"] = "";

            var loggerMock = new Mock<ILogger>();
            var dpSpTest01 = DataPointFactory.CreateDataPoints(dpConfig, loggerMock.Object)["ANONYMOUS.SP_TEST_STRING"];

            var outParams = dpSpTest01.Execute(inParams);

            Assert.Equal("teste12", outParams["@p3"]);
        }
    }
}
