using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Universe.SqlInsights.W3Api.Client.Tests
{
    [TestFixture]
    public class TestHttpProxy
    {
        [Test]
        public void ShowSystemProxy()
        {
            var systemProxy = S5ApiClientFactory.GetSystemProxy();
            Console.WriteLine($"System Proxy Type: '{systemProxy?.GetType()}'");
            Console.WriteLine($"System Proxy: '{systemProxy}'");

            var properties = systemProxy?.GetType().GetProperties();
            Console.WriteLine($"Properties:");
            foreach (var p in properties)
            {
                Console.WriteLine($"  {p.PropertyType} [{p.Name}]");
            }

        }
    }
}
