using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Code.Challenge;
using watchtower.Models;

namespace watchtower.Services {

    public interface IChallengeManager {

        /// <summary>
        /// Get all possible challenges a run can have
        /// </summary>
        /// <returns></returns>
        List<IRunChallenge> GetAll();

        /// <summary>
        /// Get the active challenges that are allowed for a certain run
        /// </summary>
        /// <returns></returns>
        List<IRunChallenge> GetActive();

        /// <summary>
        /// Get the challenges that are currently running
        /// </summary>
        /// <returns></returns>
        List<IndexedChallenge> GetRunning();
        
        /// <summary>
        /// Start a new poll using the options passed
        /// </summary>
        /// <param name="options"></param>
        void StartPoll(ChallengePollOptions options);

        /// <summary>
        /// End a currently running poll
        /// </summary>
        void EndPoll();

        ChallengePollResults? GetPollResults();

        int GetPollTimer();

        /// <summary>
        /// Add a challenge as allowed for a match
        /// </summary>
        /// <param name="challenge"></param>
        void Add(int ID);

        /// <summary>
        /// Remove a challenge as allowed for a match
        /// </summary>
        /// <param name="ID"></param>
        void Remove(int ID);

        /// <summary>
        /// Start running a challenge, and actively give out points for it
        /// </summary>
        /// <param name="ID"></param>
        void Start(int ID);

        /// <summary>
        /// Remove an actively running challenge, an no long give out points for it
        /// </summary>
        /// <param name="index"></param>
        void End(int index);

    }
}
