using Mtecl.ApiClientBuilder;
using SmartEmailingApi.Client.Messages;
using System.Threading.Tasks;

namespace SmartEmailingApi.Client
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