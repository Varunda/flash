using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Code.Constants;
using watchtower.Constants;
using watchtower.Models;

namespace watchtower.Services {

    public interface IMatchManager {

        /// <summary>
        /// Start a match, which will create the thread that speedrunners can join to view the active challenges
        /// </summary>
        Task StartMatch();

        /// <summary>
        /// End a match, cleaning up the threads made and leave the voice channel
        /// </summary>
        Task EndMatch();

        /// <summary>
        /// Start a round. If the match is already running, nothing happens
        /// </summary>
        Task StartRound();

        /// <summary>
        /// Restart an existing round. If a match is not running, nothing happens
        /// </summary>
        Task RestartRound();

        /// <summary>
        /// Clear the match, resetting everything for another match
        /// </summary>
        Task ClearMatch();

        /// <summary>
        /// Pause a currently running round
        /// </summary>
        Task PauseRound();

        /// <summary>
        /// Stop the current round. Does nothing if a round isn't running
        /// </summary>
        Task StopRound(int? winnerIndex = null);

        /// <summary>
        /// Get the current state of the round
        /// </summary>
        RoundState GetRoundState();

        /// <summary>
        /// Get the current state of the match
        /// </summary>
        MatchState GetMatchState();

        /// <summary>
        /// Set the settings used in a match
        /// </summary>
        /// <param name="settings">Settings to use in the match</param>
        void SetSettings(MatchSettings settings);

        /// <summary>
        /// Get the current settings in a match
        /// </summary>
        /// <returns></returns>
        MatchSettings GetSettings();

        void SetAutoChallengeSettings(AutoChallengeSettings settings);

        AutoChallengeSettings GetAutoChallengeSettings();

        /// <summary>
        /// Add a new character to a runner. If no runner at the index has been set, a new runner is created
        /// </summary>
        /// <param name="index">Index of the runner to add the character to</param>
        /// <param name="charName">Name of the character to add. Case insensitive</param>
        /// <returns>If the character was successfully added or not</returns>
        Task<bool> AddCharacter(int index, string charName);

        /// <summary>
        /// Remove a character from a team
        /// </summary>
        /// <param name="index">Index of the team to remove the character from</param>
        /// <param name="charName">Name of the character to be removed</param>
        void RemoveCharacter(int index, string charName);

        /// <summary>
        /// Set the name of a runner
        /// </summary>
        /// <param name="index">Index of the runner to set</param>
        /// <param name="playerName">Name of the runner to set</param>
        void SetRunnerName(int index, string? playerName);

        /// <summary>
        /// Set the score of a runner
        /// </summary>
        /// <param name="index">Index of the runner to set the score of</param>
        /// <param name="score">Score to set the runner to</param>
        void SetScore(int index, int score);

        void IncrementScore(int index);

        /// <summary>
        /// Get the score of a runner, or null if it doesn't exist
        /// </summary>
        /// <param name="index">Index of the runner to get the score of</param>
        int? GetScore(int index);

        /// <summary>
        /// Get the runner being tracked
        /// </summary>
        /// <param name="index">Index of the runner to get</param>
        TrackedPlayer? GetPlayer(int index);

        /// <summary>
        /// Get all runners in this match
        /// </summary>
        /// <returns>The list of runners</returns>
        List<TrackedPlayer> GetPlayers();

        /// <summary>
        /// Get the <c>DateTime</c> of when a match was started
        /// </summary>
        DateTime GetMatchStart();

        /// <summary>
        /// Get the <c>DateTime</c> of when the match ended, or <c>null</c> if it hasn't ended
        /// </summary>
        DateTime? GetMatchEnd();

        /// <summary>
        /// Get how many seconds a match has been running for. Not really useful if the match has not started
        /// </summary>
        int GetMatchLength();

    }

}
