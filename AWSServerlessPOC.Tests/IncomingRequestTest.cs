using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS;
using Amazon.SQS.Model;
using AutoFixture;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace AWSServerlessPOC.Tests
{
    public class IncomingRequestTest
    {
        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void PostIncomingRequest_Should_Succeed()
        {
            var mockSqsClient = new Mock<IAmazonSQS>();
            var sendMessageResponse = _fixture.Create<SendMessageResponse>();

            mockSqsClient.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(),It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(sendMessageResponse));

            var request = _fixture.Create<APIGatewayProxyRequest>();
            var context = new Mock<ILambdaContext>();

            context.Setup(x => x.Logger.LogLine(It.IsAny<string>()));

            var sut = new IncomingRequest(mockSqsClient.Object);

            var response = sut.Post(request, context.Object).Result;

            Assert.Equal(200,response.StatusCode);
            context.Verify(x=>x.Logger.LogLine(It.IsAny<string>()),Times.Exactly(2));
            mockSqsClient.Verify(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()),Times.Once);
        }

        [Fact]
        public void ListenForNewMessages_Should_Succeed()
        {
            var mockSqsClient = new Mock<IAmazonSQS>();
            var pocMessage = _fixture.Create<POCMessage>();
            var sqsMessage = new SQSEvent.SQSMessage {Body = JsonConvert.SerializeObject(pocMessage)};
            var sqsEvent = new SQSEvent {Records = new List<SQSEvent.SQSMessage>() {sqsMessage}};
            var context = new Mock<ILambdaContext>();

            context.Setup(x => x.Logger.LogLine(It.IsAny<string>()));

            var sut = new IncomingRequest(mockSqsClient.Object);

            var response = sut.ListenForNewMessages(sqsEvent, context.Object);

            Assert.IsType<POCMessage>(response);
            Assert.Equal(pocMessage.Status,response.Status);
            Assert.Equal(pocMessage.EmailAddress, response.EmailAddress);
            Assert.Equal(pocMessage.EventName, response.EventName);

            context.Verify(x => x.Logger.LogLine(It.IsAny<string>()), Times.Once);
        }
    }
}