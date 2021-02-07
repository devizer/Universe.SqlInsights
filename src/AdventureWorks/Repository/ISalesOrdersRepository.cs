using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AdventureWorks.Models;

namespace AdventureWorks.Repository
{
    public interface ISalesOrdersRepository
    {
        List<SalesOrder> GetSalesOrders(int? customerID = null);

        List<SalesOrderDetails> GetSalesOrderDetails(int orderId);
    }
}
