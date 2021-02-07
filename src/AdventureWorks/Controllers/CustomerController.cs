using System.Data.SqlClient;
using AdventureWorks.Repository;
using System.Web.Mvc;
using AdventureWorks.Utils;

namespace AdventureWorks.Controllers
{
    public class HealthCheckController : Controller
    {
        private readonly DbConnectionOptions DbOptions;

        public HealthCheckController(DbConnectionOptions dbOptions)
        {
            DbOptions = dbOptions;
        }

        public ActionResult IsOK()
        {
            using (SqlConnection con = new SqlConnection(DbOptions.ConnectionString))
            {
                con.Open();
                ErrorsSandbox.CheckHealth(con);
            }

            return this.Json(new {OK = true}, JsonRequestBehavior.AllowGet); 
        }
    }

    public class CustomerController : Controller
    {
        private ICustomerRepository _customerRepo { get; }
        private ISalesOrdersRepository _saleRepo { get; }

        public CustomerController(ICustomerRepository customerRepo, ISalesOrdersRepository saleRepo)
        {
            _customerRepo = customerRepo;
            _saleRepo = saleRepo;
        }

        public ActionResult Index()
        {
            return View(this._customerRepo.GetCustomers());
        }

        public ActionResult Details(int id)
        {
            var customer = this._customerRepo.GetCustomer(id);
            customer.Orders = _saleRepo.GetSalesOrders(id);
            return View(customer);
        }
    }
}
