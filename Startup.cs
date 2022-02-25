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
using watchtower.Realtime;
using watchtower.Services;
using Newtonsoft.Json.Linq;
using watchtower.Models;
using System.IO;
using watchtower.Services.Db;
using watchtower.Services.Hosted;
using watchtower.Services.Implementations;
using watchtower.Code.Census.Implementations;
using watchtower.Services.Queue;

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

            services.AddRazorPages();
            services.AddServerSideBlazor();

            services.AddSingleton<IRealtimeMonitor, RealtimeMonitor>();
            services.AddSingleton<IEventHandler, Realtime.EventHandler>();

            services.AddSingleton<ICharacterCollection, CharacterCollection>();
            services.AddSingleton<IItemCollection, ItemCollection>();
            services.AddSingleton<ExperienceCollection, ExperienceCollection>();

            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddSingleton<DiscordMessageQueue>();
            services.AddSingleton<ICommandBus, CommandBus>();

            services.AddSingleton<ISecondTimer, SecondTimer>();
            services.AddSingleton<IMatchManager, MatchManager>();
            services.AddSingleton<IChallengeManager, ChallengeManager>();
            services.AddSingleton<IRealtimeEventBroadcastService, RealtimeEventBroadcastService>();
            services.AddSingleton<ITwitchChatBroadcastService, TwitchChatBroadcastService>();
            services.AddSingleton<IMatchEventBroadcastService, MatchEventBroadcastService>();
            services.AddSingleton<IChallengeEventBroadcastService, ChallengeEventBroadcastService>();
            services.AddSingleton<IMatchMessageBroadcastService, MatchMessageBroadcastService>();
            services.AddSingleton<IAdminMessageBroadcastService, AdminMessageBroadcastService>();

            services.AddSingleton<IDbHelper, DbHelper>();
            services.AddSingleton<IDbCreator, DefaultDbCreator>();

            services.AddHostedService<HostedRealtimeMonitor>();
            services.AddHostedService<HostedEventProcessService>();
            services.AddHostedService<HostedTwitchEventService>();
            services.AddHostedService<HostedDiscordMessageService>();

            services.Configure<DbOptions>(Configuration.GetSection("DbOptions"));
            services.Configure<TwitchOptions>(Configuration.GetSection("Twitch"));
            services.Configure<DiscordOptions>(Configuration.GetSection("Discord"));

            services.AddSingleton<DiscordWrapper>();
            services.AddSingleton<DiscordThreadManager>();

            Console.WriteLine($"Services configured");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) { 
            if (env.IsDevelopment()) {
               app.UseDeveloperExceptionPage();
            }

            app.Use(async (context, next) => {
                context.Response.Headers.Add("Content-Security-Policy",
                    "script-src 'self' * 'unsafe-inline' 'unsafe-eval';"
                    + "style-src 'self' 'unsafe-inline'"
                );
                await next();
            });

            //app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseRouting();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute("logs", "/logs/{action}", new { controller = "LogDownload" });

                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            Console.WriteLine($"Request pipeline configured");
        }

    }
}
