using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using watchtower.Models;

namespace watchtower.Services {

    public class DiscordWrapper {

        private readonly ILogger<DiscordWrapper> _Logger;

        private readonly DiscordClient? _Discord;
        private IOptions<DiscordOptions> _DiscordOptions;
        private VoiceNextExtension? _Voice = null;

        private bool _IsConnected = false;

        public DiscordWrapper(ILogger<DiscordWrapper> logger,
            IOptions<DiscordOptions> options) {

            _Logger = logger;

            _DiscordOptions = options;

            if (_DiscordOptions.Value.Enabled == false) {
                return;
            }

            try {
                _Discord = new DiscordClient(new DiscordConfiguration() {
                    Token = _DiscordOptions.Value.ClientKey,
                    TokenType = TokenType.Bot,
                });
            } catch (Exception) {
                throw;
            }

            _Discord.Ready += Client_Ready;
            _ = StartClient(CancellationToken.None);
        }
        
        /// <summary>
        /// Return if the Discord functions are enabled or not
        /// </summary>
        public bool IsEnabled() {
            return _DiscordOptions.Value.Enabled;
        }

        /// <summary>
        /// Start the Discord bot, connecting it to the gateway and setting up the voice
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<bool> StartClient(CancellationToken cancel) {
            if (IsEnabled() == false) {
                return true;
            }

            await _Discord!.ConnectAsync();
            _Voice =  _Discord.UseVoiceNext();

            _Logger.LogInformation($"Bot started and voice setup");

            return true;
        }

        /// <summary>
        /// Disconnect the Discord bot from the gateway
        /// </summary>
        public async Task<bool> DisconnectClient() {
            if (IsEnabled() == false) {
                return true;
            }

            await _Discord!.DisconnectAsync();
            return true;
        }

        /// <summary>
        /// Check if the Discord client being wrapped is connected
        /// </summary>
        public bool IsConnected() => _IsConnected;

        /// <summary>
        /// Get the wrapped Discord client, or null if discord is not enabled
        /// </summary>
        /// <returns></returns>
        public DiscordClient? GetClient() {
            return _Discord;
        }

        private async Task Client_Ready(DiscordClient sender, ReadyEventArgs args) {
            _Logger.LogInformation($"Discord client connected");

            _IsConnected = true;

            DiscordGuild? guild = await sender.GetGuildAsync(_DiscordOptions.Value.GuildId);
            if (guild == null) {
                _Logger.LogError($"Failed to find guild {_DiscordOptions.Value.GuildId}");
                return;
            }

            DiscordChannel? channel = await sender.GetChannelAsync(_DiscordOptions.Value.ParentChannelId);
            if (channel == null) {
                _Logger.LogWarning($"Failed to find channel {_DiscordOptions.Value.ParentChannelId}");
                return;
            }
        }

    }
}
