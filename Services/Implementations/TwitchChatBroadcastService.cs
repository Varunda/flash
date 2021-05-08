using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Services.Implementations {

    public class TwitchChatBroadcastService : ITwitchChatBroadcastService {

        public event EventHandler<Ps2EventArgs<TwitchChatMessage>>? OnChatMessage;

        public void EmitMessage(TwitchChatMessage msg) {
            OnChatMessage?.Invoke(this, new Ps2EventArgs<TwitchChatMessage>(msg));
        }

    }
}
