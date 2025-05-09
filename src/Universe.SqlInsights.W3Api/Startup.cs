using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Universe.SqlInsights.NetCore;
using Universe.SqlInsights.Shared;
using Universe.SqlInsights.SqlServerStorage;
using Universe.SqlInsights.W3Api.Helpers;
using Universe.SqlInsights.W3Api.SqlInsightsIntegration;
using Universe.SqlTrace;
using static System.Net.Mime.MediaTypeNames;

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
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(GetConnectionStringByConfiguration())
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

                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("SqlServer Insights Storage");
                ICrossPlatformLogger crossLogger = new NetCoreLogger(logger);
                return new SqlServerSqlInsightsStorage(dbProviderFactory, dbOptions.ConnectionString)
                {
                    Logger = crossLogger,
                };
            });

            services.AddSingleton<SqlInsightsReport>(SqlInsightsReport.Instance);
            services.AddScoped<ActionIdHolder>();
            services.AddScoped<ExceptionHolder>();
            services.AddScoped<KeyPathHolder>();
            services.AddSingleton<ISqlInsightsConfiguration>(provider => new SqlInsightsConfiguration(Configuration));

            
            services.AddControllers(options =>
            {
                options.Filters.Add(typeof(CustomExceptionFilter));
                options.Filters.Add(typeof(CustomGroupingActionFilter));
            });
            
            Console.WriteLine($"[Startup Configuration] Need Response Compression: {NeedResponseCompression()}");
            if (NeedResponseCompression()) services.AddResponseCompression(); // x => { x.MimeTypes = CompressedMimeTypes.List; }
            
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "Universe.SqlInsights.W3Api", Version = "v1"});
            });
        }



        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, DbProviderFactory dbProviderFactory, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            app.ValidateSqlInsightsServices();

            app.Use(middleware: async delegate (HttpContext context, Func<Task> next)
            {
                if (context.Response.Headers.ContainsKey("Server"))
                    context.Response.Headers.Remove("Server");

                context.Response.Headers.Add("Server", "devizer/s5dashboard");
                await next.Invoke();

            });

            app.UseSqlInsights(); // BEFORE Exception Handler
            if (false && env.IsDevelopment())
            {
                logger.LogInformation("Activating Developer Exception Page");
                app.UseDeveloperExceptionPage();
            }
            else
            {
            }

            logger.LogInformation("Activating Production Exception Response via JsonExceptionHandlerMiddleware");
            JsonExceptionHandlerMiddlewareCustomContext appInfo = GetErrorCustomContext();
            app.UseMiddleware<JsonExceptionHandlerMiddleware>(appInfo);

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Universe.SqlInsights.W3Api v1");
                // c.RoutePrefix = s;
            });

            // app.UseHttpsRedirection();
            
            app.UseCors();

            app.UseRouting();

            if (NeedResponseCompression()) app.UseResponseCompression();
            
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
            PreJit(logger, dbProviderFactory, app.ApplicationServices.CreateScope().ServiceProvider.GetRequiredService<ISqlInsightsConfiguration>());
        }

        private string GetConnectionStringByConfiguration()
        {
            return Configuration.GetConnectionString("SqlInsights");
        }

        private JsonExceptionHandlerMiddlewareCustomContext GetErrorCustomContext()
        {
            
            JsonExceptionHandlerMiddlewareCustomContext appInfo = new JsonExceptionHandlerMiddlewareCustomContext()
            {
                { "App", $"SqlInsights Dashboard v{typeof(Startup).Assembly.GetName().Version?.ToString(3)}" },
                { "OS Platform", CrossInfo.ThePlatform },
            };

            try
            {
                SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(GetConnectionStringByConfiguration());
                appInfo.Add("SQL Server", b.DataSource);
                appInfo.Add("SQL Warehouse Database", b.InitialCatalog);
                if (b.IntegratedSecurity)
                {
                    appInfo.Add("SQL Integrated Security", true);
                    appInfo.Add("SQL Integrated Security User", GetCurrentUserName());
                }
                else
                {
                    appInfo.Add("SQL User ID", b.UserID);
                }
            }
            catch (Exception ex)
            {
                appInfo.Add("Error", $"Malformed Storage Connection String. {ex.GetExceptionDigest()}");
            }

            return appInfo;
        }

        private static string GetCurrentUserName()
        {
            
            string userName = ">Unknown<";
            string domainName = null;
            try
            {
                userName = Environment.UserName;
            }
            catch { }
            try
            {
                domainName = Environment.UserDomainName;
            }
            catch { }

            if (!string.IsNullOrEmpty(domainName)) userName = @$"{domainName}\{userName}";
            return userName;
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
            SqlTraceConfiguration.DbProvider = dbProviderFactory;
            Console.WriteLine($"[Startup Configuration] DB Provider Factory: {dbProviderFactory.GetType().Namespace}");
            return dbProviderFactory;
        }

        void PreJit(ILogger logger, DbProviderFactory dbProviderFactory, ISqlInsightsConfiguration sqlInsightsConfiguration)
        {
            var taskPreJit = Task.Run(async () =>
            {
                Stopwatch sw = Stopwatch.StartNew();
                string server = ">unknown<", db = ">unknown<";
                try
                {
                    var connectionString = GetConnectionStringByConfiguration();
                    SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(connectionString);
                    server = csb.DataSource;
                    db = csb.InitialCatalog;
                    logger.LogInformation($"Starting Pre-JIT of Storage. Server '{server}'. Database '{db}'. Factory '{dbProviderFactory.GetType().Namespace}'");
                    // var dbProviderFactory = Microsoft.Data.SqlClient.SqlClientFactory.Instance;
                    // var dbProviderFactory = System.Data.SqlClient.SqlClientFactory.Instance;
                    var history = new SqlServerSqlInsightsStorage(dbProviderFactory, connectionString)
                    {
                        Logger = new NetCoreLogger(logger),
                    };
                    history.GetAliveSessions();
                    await history.GetActionsSummaryTimestamp(0);
                    var summary = await history.GetActionsSummary(0);
                    var keyPath = summary.FirstOrDefault()?.Key ?? new SqlInsightsActionKeyPath("Pre JIT");
                    await history.GetKeyPathTimestampOfDetails(0, keyPath);
                    (await history.GetActionsByKeyPath(0, keyPath, lastN: 1)).ToList();
                    // Experimental
                    using (IDbConnection cnn = dbProviderFactory.CreateConnection())
                    {
                        cnn.ConnectionString = connectionString;
                        StringsStorage ss = new StringsStorage(cnn, null);
                        ss.AcquireString(StringKind.AppName, sqlInsightsConfiguration.AppName);
                    }
                    
                    logger.LogInformation($"Pre-jit of ISqlInsightsStorage completed in {sw.Elapsed}. Server '{server}'. Database '{db}'");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Pre-jit of ISqlInsightsStorage took {sw.Elapsed} and failed. Server '{server}'. Database '{db}'");
                    AssemblyVisualizer.Show("Assemblies after JIT of ISqlServerInsightsStorage");
                }
                finally
                {
                }
            });

            taskPreJit.ConfigureAwait(false).GetAwaiter().GetResult();
            // taskPreJit.Wait();
        }

        bool NeedResponseCompression()
        {
            return GetBooleanConfigurationValue("ResponseCompression");
        }

        private bool GetBooleanConfigurationValue(string configPath) => this.Configuration.GetBooleanValue(configPath);
    }

    public class DbOptions
    {
        public string ConnectionString { get; set; }
    }
}