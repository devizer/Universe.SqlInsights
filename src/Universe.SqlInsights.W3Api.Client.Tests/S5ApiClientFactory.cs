using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Slices.Dashboard;

namespace Universe.SqlInsights.W3Api.Client.Tests
{
    class S5ApiClientFactory
    {
        public static IWebProxy GetSystemProxy()
        {
#if NET6_0
            IWebProxy systemProxy = HttpClient.DefaultProxy;
#else
            IWebProxy systemProxy = WebProxy.GetDefaultProxy();
            WebProxy p = (WebProxy)systemProxy;
#endif

            return systemProxy;
        }

        private static Lazy<HttpClient> _HttpClient = new Lazy<HttpClient>(() =>
        {
            String proxyURL = "http://103.167.135.111:80";
            WebProxy webProxy = new WebProxy(proxyURL)
            {
                Credentials = new NetworkCredential("user", "passwrd")
            };
            // make the HttpClient instance use a proxy
            // in its requests
            HttpClientHandler httpClientHandler = new HttpClientHandler
            {
                // Proxy = webProxy
            };
            httpClientHandler.ServerCertificateCustomValidationCallback += (HttpRequestMessage message, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors errors) => true;
            var ret = new HttpClient(httpClientHandler);
            return ret;
        });

        public static S5ApiClient Create()
        {
            return new S5ApiClient(TestEnvironment.W3ApiBaseUri, _HttpClient.Value);
        }

    }
}