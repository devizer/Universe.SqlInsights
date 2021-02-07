using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Universe.CpuUsage;
using Universe.SqlInsights.Shared;
using Universe.SqlInsights.SqlServerStorage;

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
            
            services.AddSingleton<ISqlInsightsStorage>(provider =>
            {
                return new SqlServerSqlInsightsStorage(Configuration.GetConnectionString("SqlInsights"));
            });
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "Universe.SqlInsights.W3Api", Version = "v1"});
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.Use(async (context, next) =>
            {
                CpuUsageAsyncWatcher watcher = new CpuUsageAsyncWatcher();
                await next.Invoke();
                watcher.Stop();
                Console.WriteLine($"Cpu Usage by http request is {watcher.GetSummaryCpuUsage()} {context.Request.Path} {context.Request.Method}");
            });
            
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
    }
}