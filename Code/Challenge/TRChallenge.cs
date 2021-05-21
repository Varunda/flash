using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Code.Challenge {

    public class TRChallenge : IRunChallenge {

        public int ID => 18;

        public string Name => "TRaitor";

        public string Description => "Kill TR";

        public int Multiplier => 2;

        public int Duration => 120;

        public ChallengeDurationType DurationType => ChallengeDurationType.TIMED;

        public Task<bool> WasMet(KillEvent ev, PsItem? item) {
            return Task.FromResult(ev.TargetLoadoutID == Loadout.TR_ENGINEER || ev.TargetLoadoutID == Loadout.TR_HEAVY_ASSAULT
                || ev.TargetLoadoutID == Loadout.TR_INFILTRATOR || ev.TargetLoadoutID == Loadout.TR_LIGHT_ASSAULT
                || ev.TargetLoadoutID == Loadout.TR_MAX || ev.TargetLoadoutID == Loadout.TR_MEDIC);
        }

    }
}
