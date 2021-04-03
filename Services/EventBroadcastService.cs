using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Services {

    public class EventBroadcastService : IEventBroadcastService {

        private ILogger<EventBroadcastService> _Logger;

        public EventBroadcastService(ILogger<EventBroadcastService> logger) {
            _Logger = logger;
        }

        public event EventHandler<Ps2EventArgs<string>>? OnTestEvent;
        public delegate void TestEventHandler(object sender, Ps2EventArgs<string> args);

        public event EventHandler<Ps2EventArgs<KillEvent>>? OnKillEvent;
        public delegate void KillEventHandler(object sender, Ps2EventArgs<KillEvent> args);

        public event EventHandler<Ps2EventArgs<TrackedPlayer?>>? OnPlayerUpdateEvent;
        public delegate void PlayerUpdateEventHandler(object sender, Ps2EventArgs<TrackedPlayer?> args);

        public event EventHandler<Ps2EventArgs<int>>? OnTimerEvent;
        public delegate void TimerEventHandler(object sender, Ps2EventArgs<int> args);

        public event EventHandler<Ps2EventArgs<MatchState>>? OnMatchStateEvent;
        public delegate void MatchStateEvent(object sender, Ps2EventArgs<MatchState> args);
        
        public void EmitKillEvent(KillEvent ev) {
            OnKillEvent?.Invoke(this, new Ps2EventArgs<KillEvent>(ev));
        }

        public void EmitTestEvent(string msg) {
            OnTestEvent?.Invoke(this, new Ps2EventArgs<string>(msg));
        }

        public void EmitPlayerUpdateEvent(int index, TrackedPlayer? player) {
            OnPlayerUpdateEvent?.Invoke(this, new Ps2EventArgs<TrackedPlayer?>(player));
        }

        public void EmitTimerEvent(int time) {
            OnTimerEvent?.Invoke(this, new Ps2EventArgs<int>(time));
        }

        public void EmitMatchStateEvent(MatchState state) {
            OnMatchStateEvent?.Invoke(this, new Ps2EventArgs<MatchState>(state));
        }

    }
}
