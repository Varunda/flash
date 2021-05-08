using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Code.Challenge;

namespace watchtower.Models {

    /// <summary>
    /// Options used when a new challenge poll is created
    /// </summary>
    public class ChallengePollOptions {

        /// <summary>
        /// What challenges are possible for this poll
        /// </summary>
        public List<int> Possible { get; set; } = new List<int>();

        /// <summary>
        /// How many seconds the poll is open for
        /// </summary>
        public int VoteTime { get; set; } = 60;

    }
}
