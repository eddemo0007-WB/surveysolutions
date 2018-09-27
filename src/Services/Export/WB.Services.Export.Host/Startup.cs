﻿using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;
using WB.Services.Export.Host.Infra;
using WB.Services.Export.Host.Scheduler;
using WB.Services.Export.Services.Processing;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace WB.Services.Export.Host
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public IContainer ApplicationContainer { get; private set; }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvcCore()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddJsonFormatters();            

            services.AddSingleton<IDataExportProcessesService, DataExportProcessesService>();
            services.AddSingleton<IHostedService, DataExportProcessesService>(c => c.GetService<IDataExportProcessesService>() 
                as DataExportProcessesService);
            
            ServicesRegistry.Configure(services, Configuration);

            var builder = new ContainerBuilder();
            builder.Populate(services);
            builder.AddTenantApi();
            this.ApplicationContainer = builder.Build();
            
            // Create the IServiceProvider based on the container.
            return new AutofacServiceProvider(this.ApplicationContainer);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseApplicationVersion("/.version");
            app.UseMetricServer();
            app.UseMvc();
        }
    }
}
