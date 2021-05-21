using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Code.Challenge {

    public class NCChallenge : IRunChallenge {

        public int ID => 19;

        public string Name => "Voiding checks";

        public string Description => "Kill NC";

        public int Multiplier => 2;

        public int Duration => 120;

        public ChallengeDurationType DurationType => ChallengeDurationType.TIMED;

        public Task<bool> WasMet(KillEvent ev, PsItem? item) {
            return Task.FromResult(ev.TargetLoadoutID == Loadout.NC_ENGINEER || ev.TargetLoadoutID == Loadout.NC_HEAVY_ASSAULT
                || ev.TargetLoadoutID == Loadout.NC_INFILTRATOR || ev.TargetLoadoutID == Loadout.NC_LIGHT_ASSAULT
                || ev.TargetLoadoutID == Loadout.NC_MAX || ev.TargetLoadoutID == Loadout.NC_MEDIC);
        }

    }
}
