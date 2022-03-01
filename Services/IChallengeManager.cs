using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Code;
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

        /// <summary>
        /// Get the current <see cref="ChallengeMode"/> being used
        /// </summary>
        ChallengeMode GetMode();

        /// <summary>
        /// Set the current <see cref="ChallengeMode"/>
        /// </summary>
        void SetMode(ChallengeMode mode);

        /// <summary>
        /// Get the current pull results, if any, null if not
        /// </summary>
        ChallengePollResults? GetPollResults();

        /// <summary>
        /// How how much time is left on the challenge poll timer
        /// </summary>
        int GetPollTimer();

        /// <summary>
        /// Start running a challenge, and actively give out points for it
        /// </summary>
        void Start(int ID);

        /// <summary>
        /// Remove an actively running challenge, an no long give out points for it
        /// </summary>
        void End(int index);

        /// <summary>
        /// Add a challenge as an active challenge
        /// </summary>
        /// <param name="ID"></param>
        void AddActive(int ID);

        /// <summary>
        /// Remove an active challenge
        /// </summary>
        /// <param name="ID"></param>
        void RemoveActive(int ID);

        void SetGlobalOptions(GlobalChallengeOptions options);

        GlobalChallengeOptions GetGlobalOptions();

    }
}
