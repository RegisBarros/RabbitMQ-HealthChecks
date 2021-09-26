using System;
using System.Net.Mime;
using System.Text.Json;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.HealthChecks.Api.Configurations;

namespace RabbitMQ.HealthChecks.Api
{
    /*  TUTORIAL LINKS
    https://github.com/renatogroffe/RabbitMQ_HealthChecks-DotNetCore2.2
    https://renatogroffe.medium.com/net-core-2-1-asp-net-core-2-1-rabbitmq-exemplos-utilizando-mensageria-3e1427133167
    https://renatogroffe.medium.com/net-core-2-2-asp-net-core-2-2-rabbitmq-exemplos-utilizando-mensageria-deb54ce63713
    https://renatogroffe.medium.com/net-5-health-checks-exemplos-de-implementa%C3%A7%C3%A3o-em-projetos-asp-net-core-3488cc807608 */

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)        
        {
            var rabbitMQConfigurations = new RabbitMQConfigurations();
            new ConfigureFromConfigurationOptions<RabbitMQConfigurations>(Configuration.GetSection("RabbitMQConfigurations"))
                .Configure(rabbitMQConfigurations);
            
            services.AddSingleton(rabbitMQConfigurations);

            services.AddHealthChecks()
                .AddRabbitMQ(Configuration.GetConnectionString("RabbitMQ"), name: "rabbitMQ");

            services.AddHealthChecksUI()
                .AddInMemoryStorage();

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHealthChecks("/status-text");

            app.UseHealthChecks("/status-json", 
                new HealthCheckOptions()
                {
                    ResponseWriter = async (context, report) =>
                    {
                       var result = JsonSerializer.Serialize(
                           new
                           {
                               currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                               statusApplication = report.Status.ToString(),
                           });
                        
                        context.Response.ContentType = MediaTypeNames.Application.Json;
                        await context.Response.WriteAsync(result);
                    }
                }); 

            app.UseHealthChecks("/healthchecks-data-ui", new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            }); 

            app.UseHealthChecksUI(options =>
            {
                options.UIPath = "/monitor";
            });          

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
