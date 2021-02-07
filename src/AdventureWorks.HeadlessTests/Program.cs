using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdventureWorks.HeadlessTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Parallel.Invoke(() => AppLoad.RunCustomers(), () => AppLoad.RunSalesOrders());
        }
    }

    public class LinkInfo
    {
        public string Text { get; set; }
        public string Link { get; set; }
    }
}
