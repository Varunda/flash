using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using watchtower.Models;
using watchtower.Services;
using watchtower.Services.Db;

namespace watchtower {

    public class Program {

        // Is set in main
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        private static IHost _Host;
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        public static async Task Main(string[] args) {
            Console.WriteLine($"Starting at {DateTime.UtcNow}");

            _ = Task.Run(async () => {
                try {
                    IHostBuilder builder = CreateHostBuilder(args);
                    _Host = builder.Build();

                    using (IServiceScope? scope = _Host.Services.CreateScope()) {
                        if (scope != null) {
                            IDbCreator creator = scope.ServiceProvider.GetRequiredService<IDbCreator>();

                            await creator.Execute();
                            Console.WriteLine($"Executed IDbCreator");
                        }
                    }

                    await _Host.RunAsync();
                } catch (Exception ex) {
                    Console.Error.WriteLine(ex.Message);
                }
            });

            await Task.Delay(1000);

            if (_Host == null) {
                Console.Error.WriteLine($"Host is null, cannot add commands");
            } else {
                ICommandBus? commands = _Host.Services.GetService(typeof(ICommandBus)) as ICommandBus;
                if (commands == null) {
                    Console.Error.WriteLine($"Missing ICommandBus");
                }

                string line = "";
                while (line != ".close") {
                    line = Console.ReadLine();

                    if (line == ".close") {
                        break;
                    } else {
                        if (commands != null) {
                            await commands.Execute(line);
                        }
                    }
                }

                await _Host.StopAsync();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) {
            IHostBuilder? host = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureAppConfiguration(config => {
                        config.AddUserSecrets<TwitchOptions>();
                    });
                });

            Console.WriteLine($"Created host");

            return host;
        }
    }
}
