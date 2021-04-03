using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Services {

    public interface IEventBroadcastService {

        event EventHandler<Ps2EventArgs<string>>? OnTestEvent;
        void EmitTestEvent(string msg);

        event EventHandler<Ps2EventArgs<KillEvent>>? OnKillEvent;
        void EmitKillEvent(KillEvent ev);

        event EventHandler<Ps2EventArgs<int>> OnTimerEvent;
        void EmitTimerEvent(int time);

        event EventHandler<Ps2EventArgs<TrackedPlayer?>> OnPlayerUpdateEvent;
        void EmitPlayerUpdateEvent(int index, TrackedPlayer? p1);

        event EventHandler<Ps2EventArgs<MatchState>> OnMatchStateEvent;
        void EmitMatchStateEvent(MatchState state);

    }
}
