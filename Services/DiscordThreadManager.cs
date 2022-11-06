using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using watchtower.Code.Challenge;
using watchtower.Models;
using watchtower.Services.Queue;

namespace watchtower.Services {

    public class DiscordThreadManager {

        private readonly ILogger<DiscordThreadManager> _Logger;

        private readonly DiscordMessageQueue _DiscordMessageQueue;
        private readonly DiscordWrapper _DiscordWrapper;
        private readonly IOptions<DiscordOptions> _DiscordOptions;

        private readonly IMatchMessageBroadcastService _MatchMessages;
        private readonly IAdminMessageBroadcastService _AdminMessages;

        private DiscordThreadChannel? _MatchThread = null;
        private DiscordChannel? _VoiceChannel = null;
        private DiscordGuild? _Guild = null;
        private VoiceNextConnection? _VoiceConnection = null;

        private Dictionary<int, DiscordMessage> _ScoreMessages = new Dictionary<int, DiscordMessage>();
        private DiscordMessage? _ChallengeMessage = null;

        private readonly DiscordColor _ChallengeInactiveColor = new DiscordColor(20, 20, 20);
        private readonly DiscordColor _ChallengeActiveColor = new DiscordColor(0, 255, 0);

        private readonly List<DiscordColor> _Colors = new List<DiscordColor>() {
            new DiscordColor(255, 0, 0),
            new DiscordColor(0, 0, 255)
        };

        public DiscordThreadManager(ILogger<DiscordThreadManager> logger,
            DiscordMessageQueue discordMessageQueue, DiscordWrapper discordWrapper,
            IOptions<DiscordOptions> discordOptions, IAdminMessageBroadcastService adminMessages,
            IMatchMessageBroadcastService matchMessages) {

            _DiscordMessageQueue = discordMessageQueue;
            _Logger = logger;
            _DiscordWrapper = discordWrapper;
            _DiscordOptions = discordOptions;
            _AdminMessages = adminMessages;
            _MatchMessages = matchMessages;
        }

        /// <summary>
        /// Create the match thread which will be used for the duration of the match to print out useful information
        /// </summary>
        /// <returns>If the thread was successfully created</returns>
        public async Task<bool> CreateMatchThread() {
            if (_DiscordWrapper.IsEnabled() == false) {
                return true;
            }

            try {
                DSharpPlus.DiscordClient? client = _DiscordWrapper.GetClient();
                if (client == null) {
                    throw new Exception($"Discord client is null, but is enabled?");
                }

                _Guild = await client.GetGuildAsync(_DiscordOptions.Value.GuildId);
                if (_Guild == null) {
                    _AdminMessages.Log($"Unable to find Discord guild {_DiscordOptions.Value.GuildId}");
                    return false;
                }

                DiscordChannel? parentChannel = await client.GetChannelAsync(_DiscordOptions.Value.ParentChannelId);
                if (parentChannel == null) {
                    _AdminMessages.Log($"Unable to find parent channel {_DiscordOptions.Value.ParentChannelId}");
                    return false;
                }

                _VoiceChannel = await client.GetChannelAsync(_DiscordOptions.Value.VoiceChannelId);
                if (_VoiceChannel == null) {
                    _AdminMessages.Log($"Unable to find voice channel {_DiscordOptions.Value.VoiceChannelId}");
                    return false;
                }

                DiscordMessage createMessage = await parentChannel.SendMessageAsync("Creating new thread for new speedrunners match");

                _MatchThread = await createMessage.CreateThreadAsync($"Speedrunners match - {DateTime.UtcNow:u}", DSharpPlus.AutoArchiveDuration.Day);
                await _MatchThread.JoinThreadAsync();

                DiscordMessageBuilder builder = new DiscordMessageBuilder();

                DiscordEmbed embed = new DiscordEmbedBuilder()
                    .WithColor(_ChallengeInactiveColor)
                    .WithTitle("Challenge: <none>")
                    .WithDescription("")
                    .Build();

                builder.AddEmbed(embed);

                _ChallengeMessage = await _MatchThread.SendMessageAsync(builder);

                _AdminMessages.Log($"Discord bot started, connected to voice and thread created");
            } catch (Exception ex) {
                _Logger.LogError(ex, $"error creating match thread");
                _AdminMessages.Log($"error creating new thread: {ex.Message}");
                return false;
            }

            return true;
        }

        public async Task<bool> UpdateActiveChallenge(IRunChallenge challenge) {
            if (_DiscordWrapper.IsEnabled() == false) {
                return true;
            }

            if (_ChallengeMessage == null) {
                _Logger.LogWarning($"Cannot update active challenge: no challenge message");
                return false;
            }

            DiscordEmbed embed = new DiscordEmbedBuilder()
                .WithColor(_ChallengeActiveColor)
                .WithTitle($"Challenge: {challenge.Name}")
                .WithDescription(challenge.Description)
                .Build();

            try {
                await _ChallengeMessage.ModifyAsync(embed);
            } catch (Exception ex) {
                _Logger.LogError(ex, $"Cannot update active challenge");
                return false;
            }

            return true;
        }
        
        public async Task<bool> ClearActiveChallenge() {
            if (_DiscordWrapper.IsEnabled() == false) {
                return true;
            }

            if (_ChallengeMessage == null) {
                _Logger.LogWarning($"Cannot clear active challenge: no challenge message");
                return false;
            }

            DiscordEmbed embed = new DiscordEmbedBuilder()
                .WithColor(_ChallengeInactiveColor)
                .WithTitle($"Challenge: <none>")
                .WithDescription("")
                .Build();

            try {
                await _ChallengeMessage.ModifyAsync(embed);
            } catch (Exception ex) {
                _Logger.LogError(ex, $"Cannot update active challenge");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Create the score message that can be updated for a runner
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public async Task<bool> CreateScoreMessage(int index, string runnerName) {
            if (_DiscordWrapper.IsEnabled() == false) {
                return true;
            }

            if (_ScoreMessages.ContainsKey(index) == true) {
                _Logger.LogWarning($"Cannot create score message for runner {index}: message already exists");
                return false;
            }

            if (_MatchThread == null) {
                _Logger.LogWarning($"Cannot create score message for runner {index}: match thread does not exist");
                return false;
            }

            DiscordMessageBuilder builder = new DiscordMessageBuilder();

            DiscordEmbed embed = new DiscordEmbedBuilder()
                .WithColor(_Colors[index % _Colors.Count])
                .WithTitle($"{runnerName}")
                .WithDescription("0")
                .Build();

            builder.AddEmbed(embed);

            try {
                DiscordMessage msg = await _MatchThread.SendMessageAsync(builder);
                _ScoreMessages[index] = msg;
            } catch (Exception ex) {
                _Logger.LogError(ex, $"Cannot create score message for runner {index}");
                return false;
            }

            return true;
        }

        public async Task<bool> UpdateRunnerScore(int runnerIndex, string runnerName, int score) {
            if (_DiscordWrapper.IsEnabled() == false) {
                return true;
            }

            if (_ScoreMessages.TryGetValue(runnerIndex, out DiscordMessage? msg) == false) {
                _Logger.LogWarning($"Cannot update runner {runnerIndex}/{runnerName} with score {score}: messages does not contain index");
                return false;
            }

            DiscordEmbed embed = new DiscordEmbedBuilder()
                .WithColor(_Colors[runnerIndex % _Colors.Count])
                .WithTitle($"{runnerName}")
                .WithDescription($"{score}")
                .Build();

            try {
                await msg.ModifyAsync(embed);
            } catch (Exception ex) {
                _Logger.LogError(ex, $"Cannot update runner {runnerIndex}/{runnerName} with score {score}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Send a message in the match thread
        /// </summary>
        /// <param name="msg">Message to be sent</param>
        /// <returns>If the message was successfully sent or not</returns>
        public async Task<bool> SendThreadMessage(string msg) {
            if (_DiscordWrapper.IsEnabled() == false) {
                return true;
            }

            if (_MatchThread == null) {
                _Logger.LogWarning($"Cannot send thread message, thread is null");
                return false;
            }

            try {
                await _MatchThread.SendMessageAsync(msg);
            } catch (Exception ex) {
                _Logger.LogError(ex, $"error sending message to match thread");
                _AdminMessages.Log($"error creating message to match thread: {ex.Message}");
            }

            return true;
        }

        public async Task<bool> ConnectToVoice() {
            if (_DiscordWrapper.IsEnabled() == false) {
                return true;
            }

            if (_VoiceChannel == null) {
                _Logger.LogWarning($"Voice channel is null, cannot connect");
                return false;
            }

            try {
                VoiceNextExtension? voice = _DiscordWrapper.GetClient().GetVoiceNext();
                if (voice == null) {
                    _Logger.LogWarning($"Cannot get VoiceNext");
                    return false;
                }

                VoiceNextConnection? conn = voice.GetConnection(_Guild);
                if (conn != null) {
                    conn.Disconnect();
                }

                _VoiceConnection = await voice.ConnectAsync(_VoiceChannel);
                if (_VoiceConnection == null) {
                    _Logger.LogError($"failed to connect to voice");
                }
                _Logger.LogInformation($"voice connection made");
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to connect to voice");
                _AdminMessages.Log($"error connecting to voice: {ex.Message}");
                return false;
            }

            return true;
        }

        public Task<bool> DisconnectFromVoice() {
            if (_DiscordWrapper.IsEnabled() == false) {
                return Task.FromResult(true);
            }

            if (_VoiceChannel == null) {
                return Task.FromResult(true);
            }

            try {
                VoiceNextExtension? voice = _DiscordWrapper.GetClient().GetVoiceNext();
                if (voice == null) {
                    return Task.FromResult(true);
                }

                VoiceNextConnection? conn = voice.GetConnection(_Guild);
                if (conn != null) {
                    conn.Disconnect();
                }
            } catch (Exception ex) {
                _Logger.LogError(ex, "failed to disconnect from voice");
                _AdminMessages.Log($"error disconnecting to voice: {ex.Message}");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        /// <summary>
        /// Close the match thread, archiving it and locking it from being opened again
        /// </summary>
        /// <param name="reason">Optional reason the thread is being closed</param>
        /// <returns>If the operation was successful or not</returns>
        public async Task<bool> CloseThread(string? reason = null) {
            if (_DiscordWrapper.IsEnabled() == false) {
                return true;
            }

            if (_MatchThread == null) {
                return false;
            }

            await SendThreadMessage($"Thread archived {(reason != null ? $"\nreason: {reason}" : "")}");

            try {
                await _MatchThread.ModifyAsync(action => {
                    action.IsArchived = true;
                    action.Locked = false;
                });
            } catch (Exception ex) {
                _Logger.LogError(ex, $"error archiving match thread");
                _AdminMessages.Log($"failed to close match thread: {ex.Message}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Play the start noise for the challenge
        /// </summary>
        /// <returns></returns>
        public Task<bool> PlayStartNoise() {
            if (_DiscordWrapper.IsEnabled() == false) {
                return Task.FromResult(true);
            }

            string file = Directory.GetCurrentDirectory() + "/wwwroot/sounds/start_beep.mp3";
            return Playfile(file);
        }

        /// <summary>
        /// Play the end noise for the challenge
        /// </summary>
        /// <returns></returns>
        public Task<bool> PlayEndNoise() {
            if (_DiscordWrapper.IsEnabled() == false) {
                return Task.FromResult(true);
            }

            string file = Directory.GetCurrentDirectory() + "/wwwroot/sounds/end_beep.mp3";
            return Playfile(file);
        }

        /// <summary>
        /// Play an audio file in the Discord bot
        /// </summary>
        /// <param name="file">Name of the file to play</param>
        /// <returns>If playing the file was successful</returns>
        public async Task<bool> Playfile(string file) {
            if (_DiscordWrapper.IsEnabled() == false) {
                return true;
            }

            if (File.Exists(file) == false) {
                _Logger.LogWarning($"failed to play file: file '{file}' does not exist");
                return false;
            }

            if (_VoiceConnection == null) {
                _Logger.LogWarning($"failed to play file: connection is null");
                return false;
            }

            if (_VoiceConnection.IsPlaying == true) {
                await _VoiceConnection.WaitForPlaybackFinishAsync();
            }

            try {
                await _VoiceConnection.SendSpeakingAsync(true);

                ProcessStartInfo ffmpeg = new ProcessStartInfo() {
                    FileName = "ffmpeg",
                    Arguments = $@"-i ""{file}"" -ac 2 -f s16le -ar 48000 pipe:1 -loglevel quiet",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                Process? process = Process.Start(ffmpeg);
                if (process == null) {
                    _Logger.LogWarning($"Failed to start ffmpeg");
                    return false;
                }

                Stream outputStream = process.StandardOutput.BaseStream;

                VoiceTransmitSink voiceOutput = _VoiceConnection.GetTransmitSink();
                await outputStream.CopyToAsync(voiceOutput);
                await voiceOutput.FlushAsync();
                await _VoiceConnection.WaitForPlaybackFinishAsync();
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to play file '{file}'");
                return false;
            } finally {
                await _VoiceConnection.SendSpeakingAsync(false);
            }

            return true;
        }

    }
}
