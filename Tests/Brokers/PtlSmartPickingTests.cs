using Moq;
using NLog;
using Otm.Server.Broker.Ptl;
using Otm.Server.Device.Ptl;
using Otm.Shared.ContextConfig;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Otm.Tests.Brokers
{
    public class PtlSmartPickingTests
    {

        [Fact]
        public void Teste_ProcessMessage_mensagem_valida()
        {
            var configMock = new Mock<BrokerConfig>();
            var loggerMock = new Mock<ILogger>();

            byte[] body = Encoding.Default.GetBytes("P02,QuickFlow,PTL01,'004|0|20,003|0|10,001|0|30',21/02/2023 10:34:52");
            var myObj = new SmartPickingBroker(configMock.Object, loggerMock.Object);
            myObj.ProcessMessage(body);
        }

        [Fact]
        public void Teste_ProcessMessage_mensagem_vazia()
        {
            var configMock = new Mock<BrokerConfig>();
            var loggerMock = new Mock<ILogger>();

            byte[] emptyBody = Encoding.Default.GetBytes("");
            var myObj2 = new SmartPickingBroker(configMock.Object, loggerMock.Object);
            Assert.Throws<ArgumentException>(() => myObj2.ProcessMessage(emptyBody));
        }

        [Fact]
        public void Teste_ProcessMessage_mensagem_invalida()
        {
            var configMock = new Mock<BrokerConfig>();
            var loggerMock = new Mock<ILogger>();

            byte[] invalidBody = Encoding.Default.GetBytes("P02,QuickFlow,PTL01,004,10,21/02/2023 10:34:52");
            var myObj3 = new SmartPickingBroker(configMock.Object, loggerMock.Object);
            Assert.Throws<FormatException>(() => myObj3.ProcessMessage(invalidBody));
        }

    }

    public class SmartPickingBrokerTests
    {
        private readonly Mock<ILogger> loggerMock = new Mock<ILogger>();
        private readonly BrokerConfig config = new BrokerConfig();

        [Fact]
        public void ProcessMessage_ShouldGenerateListOfPendentes_Correctly()
        {
            // Arrange
            var broker = new SmartPickingBroker(config, loggerMock.Object);
            var body = Encoding.Default.GetBytes("P02,QuickFlow,PTL01,'004|0|20,003|0|10,001|0|30',21/02/2023 10:34:52");

            // Act
            broker.ProcessMessage(body);

            // Assert
            var expectedList = new List<PtlBaseClass>
            {
                new PtlBaseClass(Guid.Empty, "004", E_DisplayColor.Verde, 20),
                new PtlBaseClass(Guid.Empty, "003", E_DisplayColor.Verde, 10),
                new PtlBaseClass(Guid.Empty, "001", E_DisplayColor.Verde, 30)
            };

            throw new NotImplementedException();
            //Assert.Equal(expectedList, broker. );
        }

        
    }
}
