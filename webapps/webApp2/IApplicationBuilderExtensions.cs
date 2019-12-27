using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;

namespace webApp2
{
    public static class IApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseConsul(this IApplicationBuilder app)
        {
            var client = app.ApplicationServices.GetService<IConsulClient>();
            var logger = app.ApplicationServices.GetService<ILogger<Startup>>();

            var features = app.Properties["server.Features"] as FeatureCollection;
            var addresses = features.Get<IServerAddressesFeature>();
            var address = addresses.Addresses.First();
            logger.LogInformation($"Adress: {address}");

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

            return app;
        }

        public static IApplicationBuilder UseDemoEndpoints(this IApplicationBuilder app)
        {
            var client = app.ApplicationServices.GetService<IConsulClient>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });

                endpoints.MapGet("/new", async context => {
                    await context.Response.WriteAsync("new from app2");
                });

                endpoints.MapGet("/healthcheck", async context => {
                    await context.Response.WriteAsync("OK");
                });

                endpoints.MapGet("/app/val", async context => {
                    await context.Response.WriteAsync("OK from app2");
                });

                endpoints.MapGet("/app/val/set", async context => {
                    var putPair = new KVPair("connectionString")
                    {
                        Value = Encoding.UTF8.GetBytes("TakiConnString")
                    };
                    var putAttempt = await client.KV.Put(putPair);

                    var getPair = await client.KV.Get("apps/jpc/settings/devices");
                    await context.Response.WriteAsync(Encoding.UTF8.GetString(getPair.Response.Value, 0, getPair.Response.Value.Length));
                });
            });

            return app;
        }
    }
}
