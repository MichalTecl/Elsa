
namespace Elsa.Integration.Crm.Raynet.Model
{

    public class RnResponse
    {
        internal string OriginalJson { get; set; }

        public bool Success { get; set; }

        public override string ToString()
        {
            return OriginalJson;
        }
    }

    public class RnResponse<TData> : RnResponse
    {
        public TData Data { get; set;}
    }
}
