using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Code.Challenge {

    public class MedicChallenge : IRunChallenge {

        public int ID => 2;

        public string Name => "Medic Kills";

        public string Description => "Get kills as a medic";

        public int Duration => 300;

        public int Multiplier => 2;

        public ChallengeDurationType DurationType => ChallengeDurationType.TIMED;

        public Task<bool> WasMet(KillEvent ev, PsItem? item) {
            if (ev.LoadoutID == Loadout.NC_MEDIC || ev.LoadoutID == Loadout.TR_MEDIC || ev.LoadoutID == Loadout.VS_MEDIC) {
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

    }
}
