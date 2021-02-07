using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using Dapper;

using AdventureWorks.Models;

namespace AdventureWorks.Repository
{
    public class DapperSalesOrdersRepository: ISalesOrdersRepository
	{
        private DbConnectionOptions DbOptions { get; }

        public DapperSalesOrdersRepository(DbConnectionOptions dbOptions)
        {
            DbOptions = dbOptions;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(DbOptions.ConnectionString);
        }

        public List<SalesOrder> GetSalesOrders(int? customerID = null)
        {
            using (SqlConnection sqlConnection = this.GetConnection())
            {
                IList<SalesOrder> orderList = SqlMapper.Query<SalesOrder>(
                sqlConnection, "EXEC SalesGetSalesOrders @customerId", new {customerID}).ToList();
                sqlConnection.Close();
                return orderList.ToList();
            }
        }

        public List<SalesOrderDetails> GetSalesOrderDetails(int orderId)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@SalesOrderID", orderId);
            using (SqlConnection sqlConnection = this.GetConnection())
            {
                IList<SalesOrderDetails> orderList =
                sqlConnection.Query<SalesOrderDetails>("SalesGetSalesOrderDetails", parameters,
                    commandType: CommandType.StoredProcedure).ToList();

                return orderList.ToList();
            }
        }

    }
}
