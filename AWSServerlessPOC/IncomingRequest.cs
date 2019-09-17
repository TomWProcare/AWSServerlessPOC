using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.SQSEvents;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.Util;
using Newtonsoft.Json;
using Message = Amazon.SimpleEmail.Model.Message;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
namespace AWSServerlessPOC
{
    public class IncomingRequest
    {

        public IncomingRequest()
        {
            
        }


        /// <summary>
        /// A Lambda function to respond to HTTP Get methods from API Gateway
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The list of blogs</returns>
        public async Task<APIGatewayProxyResponse> Post(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine("ASync post request \n");
            var SQS_QUEUE_URL = Environment.GetEnvironmentVariable("MY_SQS_QUEUE_URL");
            var SQS_ACCOUNT_ID = Environment.GetEnvironmentVariable("MY_SQS_ACCOUNT_ID");
            var SQS_QUEUE_NAME = Environment.GetEnvironmentVariable("MY_SQS_QUEUE_NAME");
            var queueURL = SQS_QUEUE_URL + SQS_ACCOUNT_ID + "/" + SQS_QUEUE_NAME;
            context.Logger.LogLine("URL is: " + queueURL);
            using (var sqsClient = new Amazon.SQS.AmazonSQSClient())
            {
                var sqsRequest = new SendMessageRequest {QueueUrl = queueURL, MessageBody = request.Body};
                var sqsMessageResponse = await sqsClient.SendMessageAsync(sqsRequest);
            }

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
            //foreach (var currentMessage in sqsEvent.Records)
            //{
            //    var pocMessage = JsonConvert.DeserializeObject<POCMessage>(currentMessage.Body);
            //    context.Logger.LogLine("Received the following item from sqs queue: " + currentMessage.Body);  
            //    return pocMessage;
            //}
            var messageBody = sqsEvent.Records[0].Body;
            var pocMessage = JsonConvert.DeserializeObject<POCMessage>(messageBody);
            context.Logger.LogLine("Received the following item from sqs queue: " + messageBody);
            return pocMessage;
        }
    }
}
