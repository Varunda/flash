using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Code.Challenge {

    public class CarbineChallenge : IRunChallenge {

        public int ID => 9;

        public string Name => "Carbines";

        public string Description => "Get kills with carbines";

        public int Multiplier => 2;

        public int Duration => 120;

        public ChallengeDurationType DurationType => ChallengeDurationType.TIMED;

        public Task<bool> WasMet(KillEvent ev, PsItem? item) {
            return Task.FromResult(item != null && item.CategoryID == ItemCategory.Carbine);
        }

    }
}
