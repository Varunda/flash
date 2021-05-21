using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Code.Challenge {

    public class EngineerChallenge : IRunChallenge {

        public int ID => 11;

        public string Name => "Nuts and bolts";

        public string Description => "Get kills as an engineer";

        public int Multiplier => 2;

        public int Duration => 120;

        public ChallengeDurationType DurationType => ChallengeDurationType.TIMED;

        public Task<bool> WasMet(KillEvent ev, PsItem? item) {
            if (ev.LoadoutID == Loadout.NC_ENGINEER || ev.LoadoutID == Loadout.TR_ENGINEER || ev.LoadoutID == Loadout.VS_ENGINEER) {
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }
}
