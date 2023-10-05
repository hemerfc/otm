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
        public void Execute_DataPoint_SP_SCRIPT_HB()
        {
            /* PLSQL
             CREATE OR REPLACE PROCEDURE SP_SCRIPT_HB (
               P1  IN NUMBER,
               P2  OUT NUMBER)
               IS
               BEGIN	
               P2 := P1;	
               
               END SP_SCRIPT_HB;
             */
            
            // Prepare the DataPointConfig for SP_SCRIPT_HB
            var dpConfig = new DataPointConfig[]{
                new DataPointConfig {
                    Name = "ANONYMOUS.SP_SCRIPT_HB",
                    Driver = "oracle",
                    Config = ConnStr,
                    Params = (new DataPointParamConfig[] {
                        new DataPointParamConfig {
                            Name = "@P1",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.FromOTM
                        },
                        new DataPointParamConfig {
                            Name = "@P2",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.ToOTM,
                            Length = 9
                        },
                    }).ToList(),
                    ContextName = "ANONYMOUS"
                }
            };

            var inParams = new Dictionary<string, object>();
            inParams["@P1"] = 1;
            inParams["@P2"] = 0;

            var loggerMock = new Mock<ILogger>();
            var dpSP_SCRIPT_HB = DataPointFactory.CreateDataPoints(dpConfig, loggerMock.Object)["ANONYMOUS.SP_SCRIPT_HB"];

            var outParams = dpSP_SCRIPT_HB.Execute(inParams);

            Assert.Equal(1, outParams["@P2"]);
        }



        private static IDictionary<string, object> Prepare_DataPoint_SP_SORTER_AGUIA(IDictionary<string, object> values)
        {
            /* PLSQL
             CREATE OR REPLACE PROCEDURE SP_SORTER_AGUIA (
               BARCODE       IN  VARCHAR2,
               BARCODE_COUNT IN  INTEGER,
               CMD           OUT INTEGER,
               CMD_COUNT     OUT INTEGER
               )
               IS 
               CX_FINALIZADA VARCHAR2(13);
               BEGIN	
               CMD := 99;
               CMD_COUNT := BARCODE_COUNT;
               
               SELECT FINALIZADA
               INTO CX_FINALIZADA
               FROM RGNV_AGUIA_CARGASWM
               WHERE SCANNER = BARCODE;
               
               IF CX_FINALIZADA =  'S' THEN
               CMD := 1;	
               END IF;
               
               
               EXCEPTION
               WHEN NO_DATA_FOUND THEN
               CMD := 99;
               END SP_SORTER_AGUIA;
               
             *
             * 
             */
            // Prepare the DataPointConfig for SP_SORTER_AGUIA
            var dpConfig = new DataPointConfig[]{
                new DataPointConfig {
                    Name = "ANONYMOUS.SP_SORTER_AGUIA",
                    Driver = "oracle",
                    Config = ConnStr,
                    Params = (new DataPointParamConfig[] {
                        new DataPointParamConfig {
                            Name = "@BARCODE",
                            TypeCode = TypeCode.String,
                            Mode = Modes.FromOTM,
                            Length = 13
                        },
                        new DataPointParamConfig {
                            Name = "@BARCODE_COUNT",
                            TypeCode = TypeCode.Int16,
                            Mode = Modes.FromOTM
                        },
                        new DataPointParamConfig {
                            Name = "@CMD",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.ToOTM,
                            Length = 9
                        },
                        new DataPointParamConfig {
                            Name = "@CMD_COUNT",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.ToOTM,
                            Length = 4
                        }
                    }).ToList(),
                    ContextName = "ANONYMOUS"
                }
            };

            var loggerMock = new Mock<ILogger>();
            var dpSP_SORTER_AGUIA = DataPointFactory.CreateDataPoints(dpConfig, loggerMock.Object)["ANONYMOUS.SP_SORTER_AGUIA"];
            var result =  dpSP_SORTER_AGUIA.Execute(values);
            return result;
        }
        
        [Fact]
        public void Execute_DataPoint_SP_SORTER_AGUIA_BarcodeNotFound()
        {
            var inParams = new Dictionary<string, object>();
            inParams["@BARCODE"] = "404";
            inParams["@BARCODE_COUNT"] = 1;
            inParams["@CMD"] = 0;
            inParams["@CMD_COUNT"] = 0;
            
            var outParams = Prepare_DataPoint_SP_SORTER_AGUIA(inParams);

            Assert.Equal(99, outParams["@CMD"]); 
            Assert.Equal(1, outParams["@CMD_COUNT"]); 
        }
        
        [Fact]
        public void Execute_DataPoint_SP_SORTER_AGUIA_BarcodeNoRead()
        {
            var inParams = new Dictionary<string, object>();
            inParams["@BARCODE"] = "NoRead";
            inParams["@BARCODE_COUNT"] = 2;
            inParams["@CMD"] = 0;
            inParams["@CMD_COUNT"] = 0;
            
            var outParams = Prepare_DataPoint_SP_SORTER_AGUIA(inParams);

            Assert.Equal(99, outParams["@CMD"]); 
            Assert.Equal(2, outParams["@CMD_COUNT"]); 
        }
        
        [Fact]
        public void Execute_DataPoint_SP_SORTER_AGUIA_CaixaNaoFinalizada()
        {
            var inParams = new Dictionary<string, object>();
            inParams["@BARCODE"] = "12345"; // Inserida no banco como finalizada 'N'
            inParams["@BARCODE_COUNT"] = 3;
            inParams["@CMD"] = 0;
            inParams["@CMD_COUNT"] = 0;
            
            var outParams = Prepare_DataPoint_SP_SORTER_AGUIA(inParams);

            Assert.Equal(99, outParams["@CMD"]); 
            Assert.Equal(3, outParams["@CMD_COUNT"]); 
        }
        
        [Fact]
        public void Execute_DataPoint_SP_SORTER_AGUIA_CaixaFinalizada()
        {
            var inParams = new Dictionary<string, object>();
            inParams["@BARCODE"] = "98765"; // Inserida no banco como finalizada 'S'
            inParams["@BARCODE_COUNT"] = 4;
            inParams["@CMD"] = 0;
            inParams["@CMD_COUNT"] = 0;
            
            var outParams = Prepare_DataPoint_SP_SORTER_AGUIA(inParams);

            Assert.Equal(1, outParams["@CMD"]); 
            Assert.Equal(4, outParams["@CMD_COUNT"]); 
        }
        
        [Fact]
        public void Execute_DataPoint_SP_SORTER_AGUIA_BarcodeVazio()
        {
            var inParams = new Dictionary<string, object>();
            inParams["@BARCODE"] = ""; 
            inParams["@BARCODE_COUNT"] = 5;
            inParams["@CMD"] = 0;
            inParams["@CMD_COUNT"] = 0;
            
            var outParams = Prepare_DataPoint_SP_SORTER_AGUIA(inParams);

            Assert.Equal(99, outParams["@CMD"]); 
            Assert.Equal(5, outParams["@CMD_COUNT"]); 
        }


    }
}
