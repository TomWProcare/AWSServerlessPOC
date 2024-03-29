﻿using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS;
using Amazon.SQS.Model;
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace AWSServerlessPOC.Tests
{
    public class IncomingRequestTest
    {
        private readonly Fixture _fixture = new Fixture();

        public IncomingRequestTest()
        {
              _fixture.Customize(new AutoMoqCustomization());          
        }

        [Fact]
        public void PostIncomingRequest_Should_Succeed()
        {
            var mockSqsClient = _fixture.Freeze<Mock<IAmazonSQS>>();
            var sendMessageResponse = _fixture.Create<SendMessageResponse>();
            sendMessageResponse.HttpStatusCode = HttpStatusCode.OK;
            mockSqsClient.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(),It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(sendMessageResponse));

            var request = _fixture.Create<APIGatewayProxyRequest>();
            var context = _fixture.Freeze<Mock<ILambdaContext>>();

            var sut = new IncomingRequest(mockSqsClient.Object);

            var response = sut.Post(request, context.Object).Result;

            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(sut.ReceivedSuccessfully,response.Body);
            context.Verify(x=>x.Logger.LogLine(It.IsAny<string>()),Times.Exactly(2));
            mockSqsClient.Verify(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()),Times.Once);
        }

        [Fact]
        public void PostIncomingRequest_Should_Fail()
        {
            var mockSqsClient = _fixture.Freeze<Mock<IAmazonSQS>>();
            var sendMessageResponse = _fixture.Create<SendMessageResponse>();
            sendMessageResponse.HttpStatusCode = HttpStatusCode.InternalServerError;
            mockSqsClient.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(sendMessageResponse));

            var request = _fixture.Create<APIGatewayProxyRequest>();
            var context = _fixture.Freeze<Mock<ILambdaContext>>();

            var sut = new IncomingRequest(mockSqsClient.Object);

            var response = sut.Post(request, context.Object).Result;

            Assert.Equal((int)HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal(sut.AnErrorOccured, response.Body);
            context.Verify(x => x.Logger.LogLine(It.IsAny<string>()), Times.Exactly(2));
            mockSqsClient.Verify(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void ListenForNewMessages_Should_Succeed()
        {
            var mockSqsClient = _fixture.Freeze<Mock<IAmazonSQS>>();
            var pocMessage = _fixture.Create<POCMessage>();
            var sqsMessage = new SQSEvent.SQSMessage {Body = JsonConvert.SerializeObject(pocMessage)};
            var sqsEvent = new SQSEvent {Records = new List<SQSEvent.SQSMessage>() {sqsMessage}};
            var context = _fixture.Freeze<Mock<ILambdaContext>>();

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