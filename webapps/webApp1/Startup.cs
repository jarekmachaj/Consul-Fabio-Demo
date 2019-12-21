using System;
using System.Linq;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace webApp1
{
    public class Startup
    {
        private readonly ILogger _logger;
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940

        public Startup(ILogger<Startup> logger)
        {
             _logger = logger;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            _logger.LogInformation($"Total Services Initially: {services.Count}");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });

                endpoints.MapGet("/new", async context => {
                    await context.Response.WriteAsync("new from app1");
                });

                endpoints.MapGet("/healthcheck", async context => {
                    await context.Response.WriteAsync("OK");
                });

                endpoints.MapGet("/app/val", async context => {
                    await context.Response.WriteAsync("OK from app1");
                });
            });

            var client = new ConsulClient();

             if (app.Properties["server.Features"] is FeatureCollection features)
             {
                var addresses = features.Get<IServerAddressesFeature>();                 
                var address = addresses.Addresses.First();
                _logger.LogInformation($"Adress: {address}");                        

            
                var serviceId = Guid.NewGuid().ToString();
                var uri = new Uri(address);
                var entryAssemblyName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
                var agentReg = new AgentServiceRegistration()
                {
                    Address = "host.docker.internal",//uri.Host,
                    ID = serviceId,
                    Name = entryAssemblyName,
                    Port = uri.Port,
                    Check = new AgentServiceCheck
                    {
                        Interval = TimeSpan.FromSeconds(5),
                        DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(5),
                        HTTP = $"http://host.docker.internal:{uri.Port}/healthcheck"
                    },
                    Tags = new[] { "urlprefix-/app/val" }
                };
        
                client.Agent.ServiceRegister(agentReg).GetAwaiter().GetResult();
            }

        }
    }
}