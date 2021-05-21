using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Code.Challenge {

    public class VSChallenge : IRunChallenge {

        public int ID => 20;

        public string Name => "uwu";

        public string Description => "Kill VS";

        public int Multiplier => 2;

        public int Duration => 120;

        public ChallengeDurationType DurationType => ChallengeDurationType.TIMED;

        public Task<bool> WasMet(KillEvent ev, PsItem? item) {
            return Task.FromResult(ev.TargetLoadoutID == Loadout.VS_ENGINEER || ev.TargetLoadoutID == Loadout.VS_HEAVY_ASSAULT
                || ev.TargetLoadoutID == Loadout.VS_INFILTRATOR || ev.TargetLoadoutID == Loadout.VS_LIGHT_ASSAULT
                || ev.TargetLoadoutID == Loadout.VS_MAX || ev.TargetLoadoutID == Loadout.VS_MEDIC);
        }

    }
}
