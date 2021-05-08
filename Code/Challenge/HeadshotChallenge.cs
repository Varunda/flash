using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models.Events;

namespace watchtower.Code.Challenge {

    /// <summary>
    /// Challenge for the next headshot gives 2x the points
    /// </summary>
    public class HeadshotChallenge : IRunChallenge {

        private int _count = 0;
        private int _maxCount = 3;

        public Task<bool> WasMet(KillEvent ev) {
            if (ev.IsHeadshot == true) {
                ++_count;
            }
            return Task.FromResult(ev.IsHeadshot == true);
        }

        public int ID => 1;

        public Task<bool> IsFinished(KillEvent ev) => Task.FromResult(_count >= _maxCount);

        public string Description => $"Headshots worth {Multiplier}x";

        public int Multiplier => 2;

        public string Name => "Headshot";

        public ChallengeType Type => ChallengeType.KILLS;

    }
}
