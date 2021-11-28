using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Code.Challenge {

    public class BodyshotChallenge : IRunChallenge {

        public int ID => 22;

        public string Name => "BodyShotMasta";

        public string Description => "Get bodyshot kills";

        public int Multiplier => 2;

        public int Duration => 120;

        public ChallengeDurationType DurationType => ChallengeDurationType.TIMED;

        public Task<bool> WasMet(KillEvent ev, PsItem? item) {
            return Task.FromResult(ev.IsHeadshot == false);
        }
    }
}