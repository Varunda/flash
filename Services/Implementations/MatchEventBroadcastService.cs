using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Code.Constants;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Services.Implementations {

    public class MatchEventBroadcastService : IMatchEventBroadcastService {

        public event EventHandler<Ps2EventArgs<TrackedPlayer?>>? OnPlayerUpdateEvent;
        public void EmitPlayerUpdateEvent(int index, TrackedPlayer? player) {
            OnPlayerUpdateEvent?.Invoke(this, new Ps2EventArgs<TrackedPlayer?>(player));
        }

        public event EventHandler<Ps2EventArgs<int>>? OnTimerEvent;
        public void EmitTimerEvent(int time) {
            OnTimerEvent?.Invoke(this, new Ps2EventArgs<int>(time));
        }

        public event EventHandler<Ps2EventArgs<RoundState>>? OnRoundStateEvent;
        public void EmitRoundStateEvent(RoundState state) {
            OnRoundStateEvent?.Invoke(this, new Ps2EventArgs<RoundState>(state));
        }

        public event EventHandler<Ps2EventArgs<MatchState>>? OnMatchStateEvent;
        public void EmitMatchStateEvent(MatchState state) {
            OnMatchStateEvent?.Invoke(this, new Ps2EventArgs<MatchState>(state));
        }

        public event EventHandler<Ps2EventArgs<MatchSettings>>? OnMatchSettingsEvent;
        public void EmitMatchSettingsEvent(MatchSettings settings) {
            OnMatchSettingsEvent?.Invoke(this, new Ps2EventArgs<MatchSettings>(settings));
        }

        public event EventHandler<Ps2EventArgs<AutoChallengeSettings>>? OnAutoSettingsChange;
        public void EmitAutoSettingsChange(AutoChallengeSettings settings) {
            OnAutoSettingsChange?.Invoke(this, new Ps2EventArgs<AutoChallengeSettings>(settings));
        }

    }
}
