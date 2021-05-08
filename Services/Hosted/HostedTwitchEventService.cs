using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using watchtower.Models;

namespace watchtower.Services.Hosted {

    public class HostedTwitchEventService : IHostedService {

        private TwitchPubSub _TwitchEvents;
        private TwitchClient _TwitchClient;

        private readonly ITwitchChatBroadcastService _ChatBroadcast;

        private readonly ILogger<HostedTwitchEventService> _Logger;

        private readonly IOptions<TwitchOptions> _Options;

        public HostedTwitchEventService(ILogger<HostedTwitchEventService> logger,
            ITwitchChatBroadcastService chatBroadcast,
            ILogger<TwitchPubSub> twitchLogger, IOptions<TwitchOptions> twitchOptions) {

            _ChatBroadcast = chatBroadcast ?? throw new ArgumentNullException(nameof(chatBroadcast));

            _Logger = logger;

            _TwitchEvents = new TwitchPubSub(twitchLogger);

            ClientOptions options = new ClientOptions() { };

            WebSocketClient socket = new WebSocketClient(options);
            _TwitchClient = new TwitchClient(socket);

            _Options = twitchOptions;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            _TwitchEvents.OnPubSubServiceConnected += onServiceConnect;
            _TwitchEvents.OnListenResponse += onListenResponse;
            _TwitchEvents.OnStreamUp += onStreamUp;
            _TwitchEvents.OnStreamDown += onStreamDown;
            _TwitchEvents.OnRewardRedeemed += onRewardRedeemed;

            _TwitchClient.OnMessageReceived += onChat;
            _TwitchClient.OnConnectionError += onConnectedError;
            _TwitchClient.OnConnected += onConnected;
            _TwitchClient.OnError += onError;
            _TwitchClient.OnDisconnected += onDisconnected;

            // 36249564
            /*
            _TwitchEvents.ListenToVideoPlayback("36249564");
            _TwitchEvents.ListenToRewards("36249564");
            _TwitchEvents.ListenToRewards("103475890");
            _TwitchEvents.ListenToRewards("85277436");

            _Logger.LogTrace($"Calling connect to PubSub");
            _TwitchEvents.Connect();
            _Logger.LogDebug($"PubSub WebSocket connected");
            */

            _Logger.LogDebug($"Connecting to chat: {_Options.Value.ChatTargetChannel}");

            ConnectionCredentials creds = new ConnectionCredentials(_Options.Value.ChatUsername, _Options.Value.ChatOAuth);
            _TwitchClient.Initialize(creds);
            _TwitchClient.Connect();
            _TwitchClient.JoinChannel("varundaa");
            _TwitchClient.JoinChannel("shoctorr");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            _TwitchEvents.Disconnect();

            _TwitchEvents.OnPubSubServiceConnected -= onServiceConnect;
            _TwitchEvents.OnListenResponse -= onListenResponse;
            _TwitchEvents.OnStreamUp -= onStreamUp;
            _TwitchEvents.OnStreamDown -= onStreamDown;

            _TwitchClient.OnMessageReceived -= onChat;
            _TwitchClient.OnConnectionError -= onConnectedError;
            _TwitchClient.OnConnected -= onConnected;
            _TwitchClient.OnError -= onError;
            _TwitchClient.OnDisconnected -= onDisconnected;

            _Logger.LogInformation($"stop hosted twitch service");

            return Task.CompletedTask;
        }

        private void onConnected(object? sender, OnConnectedArgs args) {
            _Logger.LogInformation($"bot {args.BotUsername} connected to {args.AutoJoinChannel}");
        }

        private void onConnectedError(object? sender, OnConnectionErrorArgs args) {
            _Logger.LogWarning($"Error connecting chat client: {args.Error} {args.BotUsername}");
        }

        private void onError(object? sender, OnErrorEventArgs args) {
            _Logger.LogError($"{args.Exception}");
        }

        private void onDisconnected(object? sender, OnDisconnectedEventArgs args) {
            _Logger.LogWarning($"Disconnected event");
        }

        private void onChat(object? sender, OnMessageReceivedArgs args) {
            //_Logger.LogInformation($"{args.ChatMessage.Channel} {args.ChatMessage.DisplayName}> {args.ChatMessage.Message}");

            TwitchChatMessage msg = new TwitchChatMessage() {
                Channel = args.ChatMessage.Channel,
                ID = args.ChatMessage.Id,
                Message = args.ChatMessage.Message,
                UserID = args.ChatMessage.UserId,
                Username = args.ChatMessage.Username
            };

            _ChatBroadcast.EmitMessage(msg);
        }

        private void onServiceConnect(object? sender, EventArgs args) {
            _Logger.LogTrace($"Sending topics to PubSub");
            _TwitchEvents.SendTopics(_Options.Value.AccessToken);
            _Logger.LogDebug($"Sent topics to PubSub");
        }

        private void onListenResponse(object? sender, OnListenResponseArgs args) {
            if (args.Successful == false) {
                _Logger.LogWarning($"FAIL {args.Topic}: {args.Response.Error}");
            } else {
                _Logger.LogDebug($"SUCCESS {args.Topic}: {args.Response.Nonce}");
            }
        }

        private void onRewardRedeemed(object? sender, OnRewardRedeemedArgs args) {
            /*
            _Logger.LogInformation($"{args.DisplayName} redeemed {args.RewardId}/{args.RewardTitle}\n\tPrompt:{args.RewardPrompt}\n\tCost:{args.RewardCost}\n\tMessage: {args.Message}\n\tLogin: {args.Login} {args.Status} {args.TimeStamp}");

            if (args.RewardTitle == "monkaW") {
                _Challenges.Start(1);
            }
            */
        }

        private void onStreamUp(object? sender, OnStreamUpArgs args) {
            _Logger.LogInformation($"Stream up: {args.ServerTime}");
        }

        private void onStreamDown(object? sender, OnStreamDownArgs args) {
            _Logger.LogInformation($"Stream down: {args.ServerTime}");
        }

    }
}
