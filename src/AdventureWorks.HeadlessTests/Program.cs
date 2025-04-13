using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdventureWorks.HeadlessTests.External;

namespace AdventureWorks.HeadlessTests
{
    class Program
    {
        static void Main(string[] args)
        {
            new ChromeDriverInstaller().Install(true);
            AppLoad.RunCustomersLoop();
            AppLoad.RunSalesOrders();
            // Parallel.Invoke(() => AppLoad.RunCustomersLoop(), () => AppLoad.RunSalesOrders());
        }
    }

    public class LinkInfo
    {
        public string Text { get; set; }
        public string Link { get; set; }
    }
}
