using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Code.Challenge {

    public class C4Challenge : IRunChallenge {

        public int ID => 6;

        public string Name => "TO THE MOON!";

        public string Description => "Only C4 kills count for the next 2 minutes";

        public int Multiplier => 2;

        public int Duration => 120;

        public ChallengeDurationType DurationType => ChallengeDurationType.TIMED;

        private const int C4 = 432;
        private const int C4_ARX = 800623;
        private const int C4_Present = 6009782;
        
        public Task<bool> WasMet(KillEvent ev, PsItem? item) {
            return Task.FromResult(ev.WeaponID == C4 || ev.WeaponID == C4_ARX || ev.WeaponID == C4_Present);
        }

    }
}
