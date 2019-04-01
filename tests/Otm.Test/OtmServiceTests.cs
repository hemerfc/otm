using System;
using Xunit;
using Otm;
using Otm.Config;
using Moq;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using Otm.Logger;
using Otm.DataPoint;
using Otm.Device;
using Otm.Transaction;
using System.Collections.Generic;
using NLog;

namespace Otm.Test
{
    public class OtmServiceTests
    {


        [Theory]
        [MemberData(nameof(GetPassConfig))]
        public void OtmService_StartStopTest(RootConfig config)
        {

            var controller = new Mock<IMicroServiceController>().Object;

            var loggerFactory = mockLoggerFactory();

            int startCount = 0;
            int stopCount = 0;
            var mockTr = new Mock<ITransaction>();
            mockTr.Setup( x => x.Start()).Callback(() => startCount++);
            mockTr.Setup( x => x.Stop()).Callback(() => stopCount++);

            var otmService = new OtmService(
                controller,
                config,
                loggerFactory,
                mockDataPointFactory(config.DataPoints),
                mockDeviceFactory(config.Devices),
                mockTransactionFactory(config.Transactions, mockTr)
            );

            otmService.Initialize();

            Assert.Equal(1, otmService.DataPoints.Count);
            Assert.Equal(1, otmService.Devices.Count);
            Assert.Equal(1, otmService.Transactions.Count);
            
            otmService.Start();

            Assert.Equal(1, startCount);

            otmService.Stop();

            Assert.Equal(1, stopCount);            
        }

        [Theory]
        [MemberData(nameof(GetPassConfig))]
        public void OtmService_InitializeTest(RootConfig config)
        {

            var controller = new Mock<IMicroServiceController>().Object;

            var loggerFactory = mockLoggerFactory();

            var otmService = new OtmService(
                controller,
                config,
                loggerFactory,
                mockDataPointFactory(config.DataPoints),
                mockDeviceFactory(config.Devices),
                mockExceptionTransactionFactory(config.Transactions)
            );

            Assert.Throws<Exception>(() => otmService.Initialize());
        }

        private ILoggerFactory mockLoggerFactory()
        {
            var logger = new Mock<ILogger>().Object;

            var mock = new Mock<ILoggerFactory>();

            mock.Setup(x => x.GetCurrentClassLogger())
                .Returns(logger);

            var loggerFactory = mock.Object;

            return loggerFactory;
        }

        [Fact]
        public void OtmService_StopTest()
        {            
            Assert.True(true);
        }     

        private IDataPointFactory mockDataPointFactory(DataPointConfig[] config)
        {
            var dict = new Dictionary<string, IDataPoint>();
            
            foreach(var it in config)
                dict.Add(it.Name, new Mock<IDataPoint>().Object);

            var dpMock = new Mock<IDataPointFactory>();
            dpMock.Setup(x => x.CreateDataPoints(config))
                .Returns(dict);
        
            return dpMock.Object;
        }

        private IDeviceFactory mockDeviceFactory(DeviceConfig[] config)
        {
            var dict = new Dictionary<string, IDevice>();
            
            foreach(var it in config)
                dict.Add(it.Name, new Mock<IDevice>().Object);

            var dvMock = new Mock<IDeviceFactory>();
            dvMock.Setup(x => x.CreateDevices(config))
                .Returns(dict);
                
            return dvMock.Object;
        }

        private ITransactionFactory mockTransactionFactory(TransactionConfig[] config, Mock<ITransaction> mockTr)
        {
            var dict = new Dictionary<string, ITransaction>();

            foreach(var it in config)
                dict.Add(it.Name, mockTr.Object);

            var trMock = new Mock<ITransactionFactory>();
            trMock.Setup(x => 
                    x.CreateTransactions(config,
                                         It.IsAny<IDictionary<string,IDataPoint>>(), 
                                         It.IsAny<IDictionary<string,IDevice>>()))
                .Returns(dict);

            return trMock.Object;
        }

        private ITransactionFactory mockExceptionTransactionFactory(TransactionConfig[] config)
        {
            var dict = new Dictionary<string, ITransaction>();
            
            foreach(var it in config)
                dict.Add(it.Name, new Mock<ITransaction>().Object);

            var trMock = new Mock<ITransactionFactory>();
            trMock.Setup(x => 
                    x.CreateTransactions(config,
                                         It.IsAny<IDictionary<string,IDataPoint>>(), 
                                         It.IsAny<IDictionary<string,IDevice>>()))
                .Callback( ()  => throw new Exception());

            return trMock.Object;
        }

        public static IEnumerable<object[]> GetPassConfig()
        {
            return new List<object[]>()
            { 
                new object[] {
                    new RootConfig {
                        Name = "AppName",
                        DataPoints = new DataPointConfig[] {
                            new DataPointConfig { 
                                Name = "", 
                                Driver = ""
                            }
                        },
                        Devices = new DeviceConfig[] {
                            new DeviceConfig {
                                Name = "",
                                Driver = ""
                            }
                        },
                        Transactions = new TransactionConfig[] {
                            new TransactionConfig {
                                Name = ""
                            }
                        }                    
                    }
                },
            };
        }      

        public static IEnumerable<object[]> GetConfigWithoutTransaction()
        {
            return new List<object[]>()
            { 
                new object[] {
                    new RootConfig {
                        Name = "AppName",
                        DataPoints = new DataPointConfig[] {
                            new DataPointConfig { 
                                Name = "", 
                                Driver = ""
                            }
                        },
                        Devices = new DeviceConfig[] {
                            new DeviceConfig {
                                Name = "",
                                Driver = ""
                            }
                        },
                        Transactions = null                    
                    }
                },
            };
        }                      
    }
}

