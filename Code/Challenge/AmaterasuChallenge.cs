using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Code.Challenge {

    public class AmaterasuChallenge : IRunChallenge {

        public int ID => 21;

        public string Name => "Banana Fight";

        public string Description => "Get kills with the Amaterasu";

        public int Multiplier => 2;

        public int Duration => 120;

        public ChallengeDurationType DurationType => ChallengeDurationType.TIMED;

        private const int AMATERASU = 804795;

        public Task<bool> WasMet(KillEvent ev, PsItem? item) {
            return Task.FromResult(ev.WeaponID == AMATERASU);
        }

    }
}