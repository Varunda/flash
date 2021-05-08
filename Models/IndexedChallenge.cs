using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Code.Challenge;

namespace watchtower.Models {

    /// <summary>
    /// Represents a currently running challenge, along with the index it's stored at
    /// </summary>
    public class IndexedChallenge {

        private static int _PreviousIndex = 0;

        /// <summary>
        /// Index the running challenge is referenced by
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// Challenge being ran
        /// </summary>
        public IRunChallenge Challenge { get; private set; }

        public int KillCount { get; set; }

        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        public IndexedChallenge(IRunChallenge challenge) {
            Challenge = challenge;
            Index = _PreviousIndex++;
        }

    }
}
