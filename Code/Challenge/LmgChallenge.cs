using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Code.Challenge {

    public class LmgChallenge : IRunChallenge {

        public int ID => 17;

        public string Name => "Zip it sweat";

        public string Description => "Get kills with LMGs";

        public int Multiplier => 2;

        public int Duration => 300;

        public ChallengeDurationType DurationType => ChallengeDurationType.TIMED;

        public Task<bool> WasMet(KillEvent ev, PsItem? item) {
            return Task.FromResult(item != null && item.CategoryID == ItemCategory.LMG);
        }

    }
}
