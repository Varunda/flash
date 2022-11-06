using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using watchtower.Models;
using watchtower.Services.Queue;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System.Threading;
using System.Threading.Tasks;

namespace watchtower.Services.Hosted {

    public class HostedDiscordMessageService : BackgroundService {

        private readonly ILogger<HostedDiscordMessageService> _Logger;

        private readonly DiscordMessageQueue _MessageQueue;
        private readonly DiscordWrapper _DiscordWrapper;
        private IOptions<DiscordOptions> _DiscordOptions;

        private const string SERVICE_NAME = "discord";

        public HostedDiscordMessageService(ILogger<HostedDiscordMessageService> logger,
            DiscordMessageQueue msgQueue, DiscordWrapper discord,
            IOptions<DiscordOptions> options) {

            _Logger = logger;
            _MessageQueue = msgQueue ?? throw new ArgumentNullException(nameof(msgQueue));
            _DiscordWrapper = discord ?? throw new ArgumentNullException(nameof(discord));
            _DiscordOptions = options;
        }

        public async override Task StartAsync(CancellationToken cancellationToken) {
            try {
                await base.StartAsync(cancellationToken);
            } catch (Exception ex) {
                _Logger.LogError(ex, "Error in start up of DiscordService");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            if (_DiscordWrapper.IsEnabled() == false) {
                _Logger.LogInformation($"{SERVICE_NAME}> discord is disabled, not running message queue service");
                return;
            }

            _Logger.LogInformation($"Started {SERVICE_NAME}");

            while (stoppingToken.IsCancellationRequested == false) {
                try {
                    if (_DiscordWrapper.IsConnected() == false) {
                        await Task.Delay(1000, stoppingToken);
                        continue;
                    }

                    string msg = await _MessageQueue.Dequeue(stoppingToken);

                    DiscordClient? client = _DiscordWrapper.GetClient();
                    if (client == null) {
                        throw new Exception($"Client is null but Discord features are enabled?");
                    }

                    DiscordChannel? channel = await client.GetChannelAsync(_DiscordOptions.Value.ParentChannelId);
                    if (channel == null) {
                        _Logger.LogWarning($"Failed to find channel {_DiscordOptions.Value.ParentChannelId}, cannot send message");
                    } else {
                        DiscordMessageBuilder builder = new DiscordMessageBuilder();

                        builder.Content = msg;

                        DiscordMessage ret = await channel.SendMessageAsync(builder);
                    }
                } catch (Exception ex) when (stoppingToken.IsCancellationRequested == false) {
                    _Logger.LogError(ex, "Error while caching character");
                } catch (Exception) when (stoppingToken.IsCancellationRequested == true) {
                    _Logger.LogInformation($"Stopping {SERVICE_NAME} with {_MessageQueue.Count()} left");
                }
            }
        }

    }
}
