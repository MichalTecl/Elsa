using Elsa.Common.Communication;

namespace Elsa.Integration.Erp.Flox.Protocol
{
    public class DefaultResponse : IParsedResponse
    {
        public string OriginalMessage { get; set; }
        public bool Success { get; set; }
    }
}
