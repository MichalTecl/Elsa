using Mtecl.ApiClientBuilder.Abstract;

namespace Elsa.Integration.SmartEmailing.Messages
{
    public class SeResponse : IInjectResponseData
    {
        private string _responseData = null;

        public string Status { get; set; }
        public string Message { get; set; }
        public void InjectResponseData(string responseData)
        {
            _responseData = responseData;
        }

        public override string ToString()
        {
            return _responseData ?? base.ToString();
        }
    }
}
