using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Code.Challenge {

    public class ShotgunChallenge : IRunChallenge {

        public int ID => 3;

        public string Name => "GRAB THE SHOTGUN HONEY";

        public string Description => "Get kills with shotguns";

        public int Multiplier => 2;

        public int Duration => 5;

        public ChallengeDurationType DurationType => ChallengeDurationType.KILLS;

        public Task<bool> WasMet(KillEvent ev, PsItem? item) {
            return Task.FromResult(item != null && item.CategoryID == ItemCategory.Shotgun);
        }

    }
}
