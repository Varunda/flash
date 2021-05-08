using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Services {

    /// <summary>
    /// Default implementation of a <see cref="IRealtimeEventBroadcastService"/>
    /// </summary>
    public class RealtimeEventBroadcastService : IRealtimeEventBroadcastService {

        private ILogger<RealtimeEventBroadcastService> _Logger;

        public RealtimeEventBroadcastService(ILogger<RealtimeEventBroadcastService> logger) {
            _Logger = logger;
        }

        public event EventHandler<Ps2EventArgs<KillEvent>>? OnKillEvent;
        public delegate void KillEventHandler(object sender, Ps2EventArgs<KillEvent> args);
        
        public void EmitKillEvent(KillEvent ev) {
            OnKillEvent?.Invoke(this, new Ps2EventArgs<KillEvent>(ev));
        }

        public event EventHandler<Ps2EventArgs<ExpEvent>>? OnExpEvent;
        public delegate void ExpEventHandler(object sender, Ps2EventArgs<ExpEvent> args);

        public void EmitExpEvent(ExpEvent ev) {
            OnExpEvent?.Invoke(this, new Ps2EventArgs<ExpEvent>(ev));
        }

        public event EventHandler<Ps2EventArgs<string>>? OnTestEvent;
        public delegate void TestEventHandler(object sender, Ps2EventArgs<string> args);

        public void EmitTestEvent(string msg) {
            OnTestEvent?.Invoke(this, new Ps2EventArgs<string>(msg));
        }

    }
}
