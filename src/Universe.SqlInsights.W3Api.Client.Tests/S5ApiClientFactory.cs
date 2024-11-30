using System.Net.Http;
using Slices.Dashboard;

namespace Universe.SqlInsights.W3Api.Client.Tests
{
    class S5ApiClientFactory
    {
        private static HttpClient _HttpClient = new HttpClient();
        public static S5ApiClient Create()
        {
            return new S5ApiClient(TestEnvironment.W3ApiBaseUri, _HttpClient);
        }
    }
}