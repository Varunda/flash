using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Code.Challenge {

    public class InfilChallenge : IRunChallenge {

        public int ID => 7;

        public string Name => "SNEAKY BEAKY LIKE";

        public string Description => "Only kills as infil count for the next 2 minutes";

        public int Multiplier => 2;

        public int Duration => 120;

        public ChallengeDurationType DurationType => ChallengeDurationType.TIMED;

        public Task<bool> WasMet(KillEvent ev, PsItem? item) {
            if (ev.LoadoutID == Loadout.NC_INFILTRATOR || ev.LoadoutID == Loadout.TR_INFILTRATOR || ev.LoadoutID == Loadout.VS_INFILTRATOR) {
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }
}
