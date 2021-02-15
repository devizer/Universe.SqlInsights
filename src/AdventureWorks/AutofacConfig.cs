using System.Data.SqlClient;
using System.Web.Mvc;
using AdventureWorks.Repository;
using AdventureWorks.SqlInsightsIntegration;
using Autofac;
using Autofac.Integration.Mvc;
using Universe.SqlInsights.Shared;
using Universe.SqlInsights.SqlServerStorage;

namespace AdventureWorks
{
    public class AutofacConfig
    {
        public static void ConfigureContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterControllers(typeof(MvcApplication).Assembly);

            builder.RegisterType<DapperCustomerRepository>().As<ICustomerRepository>();
            builder.RegisterType<DapperSalesOrdersRepository>().As<ISalesOrdersRepository>();

            /*
         ORIGINAL
        var connectionString = ConfigurationManager.ConnectionStrings["AdventureWorks"].ToString();
        builder
            .Register(c => new DbConnectionOptions() {ConnectionString = connectionString})
            .As<DbConnectionOptions>().InstancePerRequest();
            */

            builder.Register(c => new DbConnectionOptions()
            {
                ConnectionString = ConnectionStringBuilder.BuildConnectionString()
            });

            builder
                .Register(c => new SqlServerSqlInsightsStorage(SqlClientFactory.Instance, SqlInsightsConfiguration.Instance.HistoryConnectionString))
                .As<ISqlInsightsStorage>();
        
            var container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
        }
    }
}