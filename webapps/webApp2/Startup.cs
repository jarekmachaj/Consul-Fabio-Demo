using System;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace webApp2
{
    public class Startup
    {
        private readonly ILogger _logger;
        private IConfiguration _configuration { get; set; }

        public Startup(ILogger<Startup> logger, IConfiguration configuration)
        {
             _logger = logger;
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            _logger.LogInformation($"Total Services Initially: {services.Count}");
            services.Configure<DatabaseSettings>(_configuration.GetSection("DatabaseSettings"));
            services.Configure<ConsulSettings>(_configuration.GetSection(nameof(ConsulSettings)));

            services.ConfigureAll<DatabaseSettings>(opt =>
            {
                opt.ConnectionString = "xxx";
            });


            var consulSettings = _configuration.GetSection(nameof(ConsulSettings)).Get<ConsulSettings>();
            services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
            {
                consulConfig.Address = new Uri(consulSettings.ConsulAddress);
                consulConfig.Token = consulSettings.Token;
            }));           

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseRouting();
            app.UseConsul();
            app.UseDemoEndpoints();            
        }
    }
}
