using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Code.Challenge {

    /// <summary>
    /// Challenge for the next headshot gives 2x the points
    /// </summary>
    public class HeadshotChallenge : IRunChallenge {

        public int ID => 1;

        public string Name => "SKULL CHASER";

        public string Description => "Get headshot kills";

        public int Multiplier => 2;

        public int Duration => 60;

        public ChallengeDurationType DurationType => ChallengeDurationType.TIMED;

        public Task<bool> WasMet(KillEvent ev, PsItem? item) {
            return Task.FromResult(ev.IsHeadshot == true);
        }

    }
}
