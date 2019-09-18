using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
namespace AWSServerlessPOC
{
    public class IncomingRequest
    {
        private readonly IAmazonSQS _amazonSqs;

        public IncomingRequest() : this(new AmazonSQSClient())
        {
            
        }

        public IncomingRequest(IAmazonSQS amazonSqs)
        {
            _amazonSqs = amazonSqs;
        }

        /// <summary>
        /// A Lambda function to respond to HTTP Get methods from API Gateway
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns>The list of blogs</returns>
        public async Task<APIGatewayProxyResponse> Post(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine("ASync post request \n");

            var sqsQueueUrl = Environment.GetEnvironmentVariable("MY_SQS_QUEUE_URL");
            var sqsAccountId = Environment.GetEnvironmentVariable("MY_SQS_ACCOUNT_ID");
            var sqsQueueName = Environment.GetEnvironmentVariable("MY_SQS_QUEUE_NAME");

            var queueUrl = $"{sqsQueueUrl}{sqsAccountId}/{sqsQueueName}";

            context.Logger.LogLine("URL is: " + queueUrl);

            var sqsRequest = new SendMessageRequest {QueueUrl = queueUrl, MessageBody = request.Body};
            var sqsMessageResponse = await _amazonSqs.SendMessageAsync(sqsRequest);

                var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = "Received Successfully.",
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            };

            return response;
        }

        public POCMessage ListenForNewMessages(SQSEvent sqsEvent, ILambdaContext context)
        {
            var messageBody = sqsEvent.Records[0].Body;
            var pocMessage = JsonConvert.DeserializeObject<POCMessage>(messageBody);
            context.Logger.LogLine("Received the following item from sqs queue: " + messageBody);
            return pocMessage;
        }
    }
}
