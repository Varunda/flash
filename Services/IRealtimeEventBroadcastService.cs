using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Services {

    public interface IRealtimeEventBroadcastService {

        event EventHandler<Ps2EventArgs<string>>? OnTestEvent;
        void EmitTestEvent(string msg);

        event EventHandler<Ps2EventArgs<KillEvent>>? OnKillEvent;
        void EmitKillEvent(KillEvent ev);

        event EventHandler<Ps2EventArgs<ExpEvent>>? OnExpEvent;
        void EmitExpEvent(ExpEvent ev);

    }
}
