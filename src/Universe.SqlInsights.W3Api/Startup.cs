using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Universe.SqlInsights.NetCore;
using Universe.SqlInsights.Shared;
using Universe.SqlInsights.SqlServerStorage;
using Universe.SqlInsights.W3Api.Helpers;
using Universe.SqlInsights.W3Api.SqlInsightsIntegration;

namespace Universe.SqlInsights.W3Api
{
    public class Startup
    {
        // private DbProviderFactory DbProvider = Microsoft.Data.SqlClient.SqlClientFactory.Instance;
        // private DbProviderFactory DbProvider = SqlClientFactory.Instance;
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var dbProviderFactory = AddDbProviderFactoryService(services);

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.WithOrigins(
                                "http://example.com",
                                "http://www.contoso.com")
                            .AllowAnyHeader()
                            .AllowAnyOrigin()
                            .AllowAnyMethod();
                    });
            });

            services.AddScoped<DbOptions>(provider =>
            {
                var config = provider.GetRequiredService<ISqlInsightsConfiguration>();
                var idHolder = provider.GetRequiredService<ActionIdHolder>();
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(Configuration.GetConnectionString("SqlInsights"))
                {
                    ApplicationName = string.Format(config.SqlClientAppNameFormat, idHolder.Id.ToString("N"))
                };
                return new DbOptions() {ConnectionString = builder.ConnectionString};
            });
            
            // TODO: For SQL Server we need Func<ISqlInsightsStorage>
            services.AddScoped<ISqlInsightsStorage>(provider =>
            {
                var dbOptions = provider.GetRequiredService<DbOptions>();
                if (string.IsNullOrEmpty(dbOptions.ConnectionString))
                    throw new InvalidOperationException("Misconfigured DbOptions.ConnectionString");

                return new SqlServerSqlInsightsStorage(dbProviderFactory, dbOptions.ConnectionString);
            });

            services.AddSingleton<SqlInsightsReport>(SqlInsightsReport.Instance);
            services.AddScoped<ActionIdHolder>();
            services.AddScoped<ExceptionHolder>();
            services.AddScoped<KeyPathHolder>();
            services.AddScoped<ISqlInsightsConfiguration>(provider => new SqlInsightsConfiguration(Configuration));

            
            services.AddControllers(options =>
            {
                options.Filters.Add(typeof(CustomExceptionFilter));
                options.Filters.Add(typeof(CustomGroupingActionFilter));
            });
            
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "Universe.SqlInsights.W3Api", Version = "v1"});
            });
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, DbProviderFactory dbProviderFactory, IWebHostEnvironment env, ILogger<Startup> logger)
        {

            app.ValidateSqlInsightsServices();
            app.UseSqlInsights();
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Universe.SqlInsights.W3Api v1");
                // c.RoutePrefix = s;
            });

            // app.UseHttpsRedirection();
            
            app.UseCors();

            app.UseRouting();

            app.UseDefaultFiles(new DefaultFilesOptions() { DefaultFileNames = new List<string>() { "index.html"}});
            app.UseStaticFiles();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

            // AutoFlush
            using var scope = app.ApplicationServices.CreateScope();
            var sqlInsightsConfiguration = scope.ServiceProvider.GetRequiredService<ISqlInsightsConfiguration>();
            var reportFullFileName = sqlInsightsConfiguration.ReportFullFileName;
            SqlInsightsReport.AutoFlush(reportFullFileName, 100);
            
            // TODO: START MIGRATE ON STARTUP, crash if fail
            PreJit(logger, dbProviderFactory);
        }
        
        private DbProviderFactory AddDbProviderFactoryService(IServiceCollection services)
        {
            const StringComparison ignoreCase = StringComparison.OrdinalIgnoreCase;
            var rawDbProviderFactory = Configuration.GetValue<string>("DbProviderFactory");
            DbProviderFactory dbProviderFactory = "System".Equals(rawDbProviderFactory, ignoreCase)
                ? System.Data.SqlClient.SqlClientFactory.Instance
                : "Microsoft".Equals(rawDbProviderFactory, ignoreCase)
                    ? Microsoft.Data.SqlClient.SqlClientFactory.Instance
                    : throw new ArgumentException("Invalid DbProviderFactory configuration value. 'System' or 'Microsoft are allowed'");

            services.AddSingleton<DbProviderFactory>(x => dbProviderFactory);
            Console.WriteLine($"[Startup Configuration] DbProviderFactory: {dbProviderFactory.GetType().Namespace}");
            return dbProviderFactory;
        }

        void PreJit(ILogger logger, DbProviderFactory dbProviderFactory)
        {
            Task.Run(async () =>
            {
                Stopwatch sw = Stopwatch.StartNew();
                string server = ">unknown<", db = ">unknown<";
                try
                {
                    var connectionString = Configuration.GetConnectionString("SqlInsights");
                    SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(connectionString);
                    server = csb.DataSource;
                    db = csb.InitialCatalog;
                    // var dbProviderFactory = Microsoft.Data.SqlClient.SqlClientFactory.Instance;
                    // var dbProviderFactory = System.Data.SqlClient.SqlClientFactory.Instance;
                    var history = new SqlServerSqlInsightsStorage(dbProviderFactory, connectionString);
                    history.GetAliveSessions();
                    await history.GetActionsSummaryTimestamp(0);
                    var summary = await history.GetActionsSummary(0);
                    var keyPath = summary.FirstOrDefault()?.Key ?? new SqlInsightsActionKeyPath();
                    await history.GetKeyPathTimestampOfDetails(0, keyPath);
                    await history.GetActionsByKeyPath(0, keyPath, lastN: 1);
                    logger.LogInformation($"Pre-jit of ISqlInsightsStorage completed in {sw.Elapsed}. Server '{server}'. Database '{db}'");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Pre-jit of ISqlInsightsStorage took {sw.Elapsed} and failed.  Server '{server}'. Database '{db}'");
                }
                finally
                {
                    AssemblyVisualizer.Show("Assemblies after JET");
                }
            });
        }

    }

    public class DbOptions
    {
        public string ConnectionString { get; set; }
    }
}