using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models.Events;

namespace watchtower.Code.Challenge {

    /// <summary>
    /// How does this challenge end?
    /// </summary>
    public enum ChallengeType {

        /// <summary>
        /// The challenge is timed and will went after a period of time
        /// </summary>
        TIMED,

        /// <summary>
        /// The challenge will end after a number of kills have been reached
        /// </summary>
        KILLS 

    }

    public interface IRunChallenge {

        /// <summary>
        /// Check if a kill grants the score multiplier
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        Task<bool> WasMet(KillEvent ev);

        /// <summary>
        /// Check if a kill finishes the challenge
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        Task<bool> IsFinished(KillEvent ev);

        /// <summary>
        /// Unique ID of the challenge. Unique per challenge, not per instance
        /// </summary>
        int ID { get; }

        /// <summary>
        /// Name of the challenge
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Description about the challenge
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Score multiplier
        /// </summary>
        int Multiplier { get; }

        ChallengeType Type { get; }

    }
}
