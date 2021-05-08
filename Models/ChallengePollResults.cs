using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Code.Challenge;

namespace watchtower.Models {

    /// <summary>
    /// Contains information about a single challenge being voted on
    /// </summary>
    public class ChallengePollResult {

        /// <summary>
        /// ID of the challenge this result is for
        /// </summary>
        public int ChallengeID { get; set; }

        /// <summary>
        /// Challenge this result is for
        /// </summary>
        public IRunChallenge Challenge { get; set; }

        /// <summary>
        /// List of users who have voted for this challenge option
        /// </summary>
        public List<string> Users { get; set; } = new List<string>();

        public ChallengePollResult(IRunChallenge challenge) {
            this.Challenge = challenge;
        }

    }

    public class ChallengePollResults {

        /// <summary>
        /// Possible options for a poll
        /// </summary>
        public Dictionary<int, ChallengePollResult> Options = new Dictionary<int, ChallengePollResult>();

        /// <summary>
        /// Challenge ID of the challenge that won the poll
        /// </summary>
        public Int32? WinnerChallengeID { get; set; }

    }

}
