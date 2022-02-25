using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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
            try {
                if (_MatchThread != null) {
                    await _MatchThread.LeaveThreadAsync();
                    _MatchThread = null;
                }

                _Guild = await _DiscordWrapper.GetClient().GetGuildAsync(_DiscordOptions.Value.GuildId);
                if (_Guild == null) {
                    _AdminMessages.Log($"Unable to find Discord guild {_DiscordOptions.Value.GuildId}");
                    return false;
                }

                DiscordChannel? parentChannel = await _DiscordWrapper.GetClient().GetChannelAsync(_DiscordOptions.Value.ParentChannelId);
                if (parentChannel == null) {
                    _AdminMessages.Log($"Unable to find parent channel {_DiscordOptions.Value.ParentChannelId}");
                    return false;
                }

                _VoiceChannel = await _DiscordWrapper.GetClient().GetChannelAsync(_DiscordOptions.Value.VoiceChannelId);
                if (_VoiceChannel == null) {
                    _AdminMessages.Log($"Unable to find voice channel {_DiscordOptions.Value.VoiceChannelId}");
                    return false;
                }

                DiscordMessage createMessage = await parentChannel.SendMessageAsync("Creating new thread for new speedrunners match");

                _MatchThread = await createMessage.CreateThreadAsync($"Speedrunners match - {DateTime.UtcNow:u}", DSharpPlus.AutoArchiveDuration.Day);
                await _MatchThread.JoinThreadAsync();

                _AdminMessages.Log($"Discord bot started, connected to voice and thread created");
            } catch (Exception ex) {
                _Logger.LogError(ex, $"error creating match thread");
                _AdminMessages.Log($"error creating new thread: {ex.Message}");
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

                await voice.ConnectAsync(_VoiceChannel);
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to connect to voice");
                _AdminMessages.Log($"error connecting to voice: {ex.Message}");
            }

            return true;
        }

        public async Task<bool> DisconnectFromVoice() {
            if (_VoiceChannel == null) {
                return true;
            }

            try {
                VoiceNextExtension? voice = _DiscordWrapper.GetClient().GetVoiceNext();
                if (voice == null) {
                    return true;
                }

                VoiceNextConnection? conn = voice.GetConnection(_Guild);
                if (conn != null) {
                    conn.Disconnect();
                }
            } catch (Exception ex) {
                _Logger.LogError(ex, "failed to disconnect from voice");
                _AdminMessages.Log($"error disconnecting to voice: {ex.Message}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Close the match thread, archiving it and locking it from being opened again
        /// </summary>
        /// <param name="reason">Optional reason the thread is being closed</param>
        /// <returns>If the operation was successful or not</returns>
        public async Task<bool> CloseThread(string? reason = null) {
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
            string file = Directory.GetCurrentDirectory() + "/wwwroot/sounds/start_beep.mp3";
            return Playfile(file);
        }

        /// <summary>
        /// Play the end noise for the challenge
        /// </summary>
        /// <returns></returns>
        public Task<bool> PlayEndNoise() {
            string file = Directory.GetCurrentDirectory() + "/wwwroot/sounds/end_beep.mp3";
            return Playfile(file);
        }

        /// <summary>
        /// Play an audio file in the Discord bot
        /// </summary>
        /// <param name="file">Name of the file to play</param>
        /// <returns>If playing the file was successful</returns>
        public async Task<bool> Playfile(string file) {
            if (File.Exists(file) == false) {
                _Logger.LogWarning($"failed to play file: file '{file}' does not exist");
                return false;
            }

            if (_VoiceChannel == null) {
                _Logger.LogWarning($"failed to play file: voice channel is null");
                return false;
            }

            VoiceNextExtension? voice = _DiscordWrapper.GetClient().GetVoiceNext();
            if (voice == null) {
                _Logger.LogWarning($"failed to play file: VoiceNext is null");
                return false;
            }

            VoiceNextConnection? conn = voice.GetConnection(_Guild);
            if (conn == null) {
                _Logger.LogWarning($"failed to play file: connection is null");
                return false;
            }

            if (conn.IsPlaying == true) {
                await conn.WaitForPlaybackFinishAsync();
            }

            try {
                await conn.SendSpeakingAsync(true);

                ProcessStartInfo ffmpeg = new ProcessStartInfo() {
                    FileName = "ffmpeg",
                    Arguments = $@"-i ""{file}"" -ac 2 -f s16le -ar 48000 pipe:1 -loglevel quiet",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                Process process = Process.Start(ffmpeg);
                Stream outputStream = process.StandardOutput.BaseStream;

                VoiceTransmitSink voiceOutput = conn.GetTransmitSink();
                await outputStream.CopyToAsync(voiceOutput);
                await voiceOutput.FlushAsync();
                await conn.WaitForPlaybackFinishAsync();
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to play file '{file}'");
                return false;
            } finally {
                await conn.SendSpeakingAsync(false);
            }

            return true;
        }

    }
}
