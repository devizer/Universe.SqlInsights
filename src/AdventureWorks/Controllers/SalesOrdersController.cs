using System.Web.Mvc;
using AdventureWorks.Repository;


namespace AdventureWorks.Controllers
{
    public class SalesOrdersController : Controller
    {
        private ISalesOrdersRepository _ordersRepo { get; }

        public SalesOrdersController(ISalesOrdersRepository ordersRepo)
        {
            _ordersRepo = ordersRepo;
        }

        // GET: SalesOrders
        public ActionResult Index()
        {
            return View(this._ordersRepo.GetSalesOrders());
        }

        // GET: SalesOrderDetails
        public ActionResult SalesOrderDetails(int orderId)
        {
            return View( this._ordersRepo.GetSalesOrderDetails(orderId) );
        }


    }
}