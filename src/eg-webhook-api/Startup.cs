using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft .ApplicationInsights.Extensibility;
//using Microsoft.ApplicationInsights.Extensibility.HostingStartup;


namespace eg_webhook_api {
    public class Startup {
        public Startup (IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices (IServiceCollection services) {
            services.AddControllers ();
            services.AddApplicationInsightsTelemetry ();
            services.AddHttpClient();

            //services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
            //        {
            //            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.EventHubs");
            //        });

            //services.AddSingleton<ITelemetryModule, FileDiagnosticsTelemetryModule>();
            //services.ConfigureTelemetryModule<FileDiagnosticsTelemetryModule>( (module, options) => {
            //    module.LogFilePath = @"C:\AISDKLOGS";
            //    module.LogFileName = "LOGS.txt";
            //    module.Severity = "Verbose";
            //} );
           
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure (IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger) {
            if (env.IsDevelopment ()) {
                app.UseDeveloperExceptionPage ();
            }

            //app.UseHttpsRedirection();

            app.UseRouting ();

            app.UseAuthorization ();

            app.UseEndpoints (endpoints => {
                endpoints.MapControllers ();
            });


        }
    }
}