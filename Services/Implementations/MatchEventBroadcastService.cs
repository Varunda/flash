using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Services.Implementations {

    public class MatchEventBroadcastService : IMatchEventBroadcastService {

        public event EventHandler<Ps2EventArgs<TrackedPlayer?>>? OnPlayerUpdateEvent;
        public delegate void PlayerUpdateEventHandler(object sender, Ps2EventArgs<TrackedPlayer?> args);

        public void EmitPlayerUpdateEvent(int index, TrackedPlayer? player) {
            OnPlayerUpdateEvent?.Invoke(this, new Ps2EventArgs<TrackedPlayer?>(player));
        }

        public event EventHandler<Ps2EventArgs<int>>? OnTimerEvent;
        public delegate void TimerEventHandler(object sender, Ps2EventArgs<int> args);

        public void EmitTimerEvent(int time) {
            OnTimerEvent?.Invoke(this, new Ps2EventArgs<int>(time));
        }

        public event EventHandler<Ps2EventArgs<MatchState>>? OnMatchStateEvent;
        public delegate void MatchStateEvent(object sender, Ps2EventArgs<MatchState> args);

        public void EmitMatchStateEvent(MatchState state) {
            OnMatchStateEvent?.Invoke(this, new Ps2EventArgs<MatchState>(state));
        }

        public event EventHandler<Ps2EventArgs<MatchSettings>>? OnMatchSettingsEvent;
        public delegate void MatchSettingsEvent(object sender, Ps2EventArgs<MatchSettings> args);

        public void EmitMatchSettingsEvent(MatchSettings settings) {
            OnMatchSettingsEvent?.Invoke(this, new Ps2EventArgs<MatchSettings>(settings));
        }

    }
}
