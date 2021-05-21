using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Code.Challenge {

    public class LightAssaultChallenge : IRunChallenge {

        public int ID => 13;

        public string Name => "LA";

        public string Description => "Get kills as LA";

        public int Multiplier => 2;

        public int Duration => 120;

        public ChallengeDurationType DurationType => ChallengeDurationType.TIMED;

        public Task<bool> WasMet(KillEvent ev, PsItem? item) {
            return Task.FromResult(true);
        }
    }
}
