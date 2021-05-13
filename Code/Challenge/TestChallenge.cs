using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Code.Challenge {

    public class TestChallenge : IRunChallenge {

        public int ID => 0;

        public string Name => "Test";

        public string Description => "Test challenge";

        public int Multiplier => new Random().Next(5);

        public int Duration => 20;

        public ChallengeDurationType DurationType => ChallengeDurationType.KILLS;

        public Task<bool> WasMet(KillEvent ev, PsItem? item) {
            return Task.FromResult(true);
        }

    }
}
