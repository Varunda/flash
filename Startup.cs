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
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Logging;
using watchtower.Census;
using watchtower.Hubs;
using watchtower.Realtime;
using watchtower.Services;
using Newtonsoft.Json.Linq;
using watchtower.Models;
using System.IO;

namespace watchtower {

    public class Startup {

        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddRouting();

            services.AddCensusServices(options => {
                options.CensusServiceId = "asdf";
                options.CensusServiceNamespace = "ps2";
                //options.LogCensusErrors = true;
            });

            /*
            services.AddSignalR(options => {
                options.EnableDetailedErrors = true;
            });
            */

            services.AddRazorPages();
            services.AddServerSideBlazor();

            services.AddSingleton<IRealtimeMonitor, RealtimeMonitor>();
            services.AddSingleton<IEventHandler, Realtime.EventHandler>();
            services.AddSingleton<ICharacterCollection, CharacterCollection>();
            services.AddSingleton<IItemCollection, ItemCollection>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddSingleton<ICommandBus, CommandBus>();

            services.AddSingleton<IMatchManager, MatchManager>();
            services.AddSingleton<IEventBroadcastService, EventBroadcastService>();

            services.AddHostedService<HostedRealtimeMonitor>();
            services.AddHostedService<EventProcessService>();

            Console.WriteLine($"Services configured");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) { 

            if (env.IsDevelopment()) {
               app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseRouting();
            app.UseEndpoints(endpoints => {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");

                endpoints.MapHub<DataHub>("/ws/data");
            });

            Console.WriteLine($"Request pipeline configured");
        }

    }
}
