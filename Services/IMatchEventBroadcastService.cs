﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Code.Constants;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Services {

    public interface IMatchEventBroadcastService {

        event EventHandler<Ps2EventArgs<int>>? OnTimerEvent;
        void EmitTimerEvent(int time);

        event EventHandler<Ps2EventArgs<TrackedPlayer?>>? OnPlayerUpdateEvent;
        void EmitPlayerUpdateEvent(int index, TrackedPlayer? p1);

        event EventHandler<Ps2EventArgs<RoundState>>? OnRoundStateEvent;
        void EmitRoundStateEvent(RoundState state);

        event EventHandler<Ps2EventArgs<MatchState>>? OnMatchStateEvent;
        void EmitMatchStateEvent(MatchState state);

        event EventHandler<Ps2EventArgs<MatchSettings>>? OnMatchSettingsEvent;
        void EmitMatchSettingsEvent(MatchSettings settings);

        event EventHandler<Ps2EventArgs<AutoChallengeSettings>> OnAutoSettingsChange;
        void EmitAutoSettingsChange(AutoChallengeSettings settings);

    }
}
