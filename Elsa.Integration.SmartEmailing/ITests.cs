using Elsa.Integration.SmartEmailing.Messages;
using Mtecl.ApiClientBuilder;
using System.Threading.Tasks;

namespace Elsa.Integration.SmartEmailing
{
    public interface ITests
    {

        [Get("/api/v3/ping")]
        Task<SeResponse> Ping();

        [Get("/api/v3/check-credentials")]
        Task<LoginTestResponse> GetLoginTest();

        [Post("/api/v3/check-credentials")]
        Task<LoginTestResponse> PostLoginTest();
    }
}