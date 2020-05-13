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
using System.ComponentModel;
using System.Threading;

namespace Otm.Test
{
    public class OtmServiceTests
    {
        [Theory]
        [MemberData(nameof(GetPassConfig))]
        public void OtmService_StartStopTest(RootConfig config)
        {

            var controller = new Mock<IMicroServiceController>().Object;

            MockTransacion.CountReset();

            var otmService = new OtmService(
                controller,
                config,
                MockDataPointFactory(config.DataPoints.ToArray()),
                MockDeviceFactory(config.Devices.ToArray()),
                MockTransactionFactory(config.Transactions.ToArray())
            );

            otmService.Initialize();

            Assert.Equal(1, otmService.DataPoints.Count);
            Assert.Equal(1, otmService.Devices.Count);
            Assert.Equal(1, otmService.Transactions.Count);
            
            otmService.Start();

            // delay because of BackgroundWorder... not the best way
            Thread.Sleep(10);

            Assert.Equal(1, MockTransacion.StartCount);

            otmService.Stop();

            Thread.Sleep(10);

            Assert.Equal(1, MockTransacion.StopCount);
        }

        [Theory]
        [MemberData(nameof(GetPassConfig))]
        public void OtmService_InitializeTest(RootConfig config)
        {
            var controller = new Mock<IMicroServiceController>().Object;

            var otmService = new OtmService(
                controller,
                config,
                MockDataPointFactory(config.DataPoints.ToArray()),
                MockDeviceFactory(config.Devices.ToArray()),
                MockExceptionTransactionFactory(config.Transactions.ToArray())
            );

            Assert.Throws<Exception>(() => otmService.Initialize());
        }

        [Fact]
        public void OtmService_StopTest()
        {            
            Assert.True(true);
        }     

        private IDataPointFactory MockDataPointFactory(DataPointConfig[] config)
        {
            var dict = new Dictionary<string, IDataPoint>();
            
            foreach(var it in config)
                dict.Add(it.Name, new Mock<IDataPoint>().Object);

            var dpMock = new Mock<IDataPointFactory>();
            dpMock.Setup(x => x.CreateDataPoints(It.IsAny<DataPointConfig[]>()))
                .Returns(dict);
        
            return dpMock.Object;
        }

        private IDeviceFactory MockDeviceFactory(DeviceConfig[] config)
        {
            var dict = new Dictionary<string, IDevice>();
            
            foreach(var it in config)
                dict.Add(it.Name, new Mock<IDevice>().Object);

            var dvMock = new Mock<IDeviceFactory>();
            
            dvMock.Setup(x => x.CreateDevices(It.IsAny<DeviceConfig[]>()))
                .Returns(dict);
                
            return dvMock.Object;
        }

        private ITransactionFactory MockTransactionFactory(TransactionConfig[] config)
        {
            var dict = new Dictionary<string, ITransaction>();

            foreach(var it in config)
                dict.Add(it.Name, new MockTransacion());

            var trMock = new Mock<ITransactionFactory>();
            trMock.Setup(x => 
                    x.CreateTransactions(config,
                                         It.IsAny<IDictionary<string,IDataPoint>>(), 
                                         It.IsAny<IDictionary<string,IDevice>>()))
                .Returns(dict);

            return trMock.Object;
        }

        private ITransactionFactory MockExceptionTransactionFactory(TransactionConfig[] config)
        {
            var dict = new Dictionary<string, ITransaction>();
            
            foreach(var it in config)
                dict.Add(it.Name, new Mock<ITransaction>().Object);

            var trMock = new Mock<ITransactionFactory>();
            trMock.Setup(x => 
                    x.CreateTransactions(config,
                                         It.IsAny<IDictionary<string,IDataPoint>>(), 
                                         It.IsAny<IDictionary<string,IDevice>>()))
                .Callback(()  => throw new Exception());

            return trMock.Object;
        }

        public static IEnumerable<object[]> GetPassConfig()
        {
            return new List<object[]>()
            { 
                new object[] {
                    new RootConfig {
                        Name = "AppName",
                        DataPoints = new List<DataPointConfig> {
                            new DataPointConfig { 
                                Name = "", 
                                Driver = ""
                            }
                        },
                        Devices = new List<DeviceConfig>{
                            new DeviceConfig {
                                Name = "",
                                Driver = ""
                            }
                        },
                        Transactions = new List<TransactionConfig> {
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
                        DataPoints = new List<DataPointConfig> {
                            new DataPointConfig { 
                                Name = "", 
                                Driver = ""
                            }
                        },
                        Devices = new List<DeviceConfig> {
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

        public class MockTransacion : ITransaction 
        {
            public BackgroundWorker Worker { get; private set;}

            public string Name => "";

            public static int StartCount { get; private set; }
            public static int StopCount { get; private set; }
            public static void CountReset()
            {
                StartCount = 0;
                StopCount = 0;
            }

            public void Start(BackgroundWorker worker)
            {
                StartCount++;
                Worker = worker;
                while(true)
                {
                    if (worker.CancellationPending)
                    {
                        Stop();
                        return;
                    }
                }
            }

            public void Stop()
            {
                StopCount++;
            }
        }       
    }
}

