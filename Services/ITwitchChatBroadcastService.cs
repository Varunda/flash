using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Services {

    public interface ITwitchChatBroadcastService {

        event EventHandler<Ps2EventArgs<TwitchChatMessage>>? OnChatMessage;
        void EmitMessage(TwitchChatMessage msg);

    }
}
