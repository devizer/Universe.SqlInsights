using System;
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
using Universe.SqlInsights.W3Api.SqlInsightsIntegration;

namespace Universe.SqlInsights.W3Api
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
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
            services.AddScoped<ISqlInsightsStorage>(provider =>
            {
                var dbOptions = provider.GetRequiredService<DbOptions>();
                if (string.IsNullOrEmpty(dbOptions.ConnectionString))
                    throw new InvalidOperationException("Misconfigured DbOptions.ConnectionString");
                
                return new SqlServerSqlInsightsStorage(SqlClientFactory.Instance, dbOptions.ConnectionString);
            });

            services.AddSingleton<SqlInsightsReport>(new SqlInsightsReport());
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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            PreJit(logger);
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

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }

        void PreJit(ILogger logger)
        {
            Task.Run(async () =>
            {
                Stopwatch sw = Stopwatch.StartNew();
                try
                {
                    var history = new SqlServerSqlInsightsStorage(SqlClientFactory.Instance, Configuration.GetConnectionString("SqlInsights"));
                    history.GetAliveSessions();
                    await history.GetActionsSummaryTimestamp(0);
                    var summary = await history.GetActionsSummary(0);
                    var keyPath = summary.FirstOrDefault()?.Key ?? new SqlInsightsActionKeyPath();
                    await history.GetKeyPathTimestampOfDetails(0, keyPath);
                    await history.GetActionsByKeyPath(0, keyPath);
                    logger.LogInformation($"Pre-jit of ISqlInsightsStorage completed in {sw.Elapsed}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Pre-jit of ISqlInsightsStorage tool {sw.Elapsed} and failed");
                }
            });
        }

    }

    public class DbOptions
    {
        public string ConnectionString { get; set; }
    }
}