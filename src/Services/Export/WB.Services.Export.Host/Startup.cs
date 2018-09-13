﻿using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Prometheus;
using WB.Services.Export.Host.Infra;
using WB.Services.Export.Host.Scheduler;
using WB.Services.Export.Infrastructure;
using WB.Services.Export.Infrastructure.Implementation;
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

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {          
            services.AddMvcCore()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddJsonFormatters();            

            services.Configure<BackgroundJobsConfig>(Configuration.GetSection("Scheduler"));
            
            services.AddScoped<IBackgroundJobClient, BackgroundJobClient>();
            services.AddSingleton<IHostedService, BackgroundJobsService>();
            services.AddTransient<IFileSystemAccessor, FileSystemAccessor>();
            services.AddTransient<ICsvWriter, CsvWriter>();
            services.AddSingleton<ICache, Cache>();
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
