using Amazon.SimpleEmail.Model;

namespace AWSServerlessPOC
{
    public class POCMessage
    {
        public string EmailAddress { get; set; }
        public string EventName { get; set; }
        public int Status { get; set; }
    }
}