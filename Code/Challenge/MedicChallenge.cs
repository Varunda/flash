using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Constants;
using watchtower.Models.Events;

namespace watchtower.Code.Challenge {

    public class MedicChallenge : IRunChallenge {

        public int ID => 2;

        public string Name => "Medic Kills";

        public string Description => "Kills as a medic give 2x points";

        public int Multiplier => 2;

        public ChallengeType Type => ChallengeType.TIMED;

        public Task<bool> IsFinished(KillEvent ev) {
            return Task.FromResult(false);
        }

        public Task<bool> WasMet(KillEvent ev) {
            if (ev.LoadoutID == Loadout.NC_MEDIC || ev.LoadoutID == Loadout.TR_MEDIC || ev.LoadoutID == Loadout.VS_MEDIC) {
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

    }
}
