using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using AdventureWorks.Models;

namespace AdventureWorks.Repository
{
    public class DapperCustomerRepository: ICustomerRepository
    {
        private DbConnectionOptions DbOptions { get; }

        public DapperCustomerRepository(DbConnectionOptions dbOptions)
        {
            DbOptions = dbOptions;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(DbOptions.ConnectionString);
        }
        
        public List<Customer> GetCustomers()
        {
            using (SqlConnection sqlConnection = this.GetConnection())
            {
                IList<Customer> customerList =
                sqlConnection.Query<Customer>(
                @"SELECT TOP 1000 CustomerID, Title, FirstName, LastName
                        FROM Sales.Customer
                                JOIN Person.Person
                                ON Sales.Customer.PersonID = Person.Person.BusinessEntityID
                            WHERE Title IS NOT NULL").ToList();
                sqlConnection.Close();
                return customerList.ToList();
            }
        }

        public Customer GetCustomer(int customerId)
        {
            using (SqlConnection sqlConnection = this.GetConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@CustomerID", customerId);
                Customer customer =
                sqlConnection.QueryFirst<Customer>(
                @"SELECT Sales.Customer.CustomerID, Title, FirstName, LastName
                        FROM Sales.Customer 
                                JOIN Person.Person
                                ON Sales.Customer.PersonID = Person.Person.BusinessEntityID
                        JOIN Sales.SalesOrderHeader
                                ON Sales.Customer.CustomerID = Sales.SalesOrderHeader.CustomerID 
                    WHERE Sales.Customer.CustomerID = @CustomerID", parameters
                );
                
                return customer;
            }
        }
    }
}
