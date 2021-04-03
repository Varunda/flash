using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Constants;
using watchtower.Models;

namespace watchtower.Services {

    public interface IMatchManager {

        /// <summary>
        /// Start a match. If the match is already running, nothing happens
        /// </summary>
        void Start();

        /// <summary>
        /// Restart an existing match. If a match is not running, nothing happens
        /// </summary>
        void Restart();

        /// <summary>
        /// Reset a match, clearing the runners
        /// </summary>
        void Reset();

        /// <summary>
        /// Pause a currently running match
        /// </summary>
        void Pause();

        /// <summary>
        /// Stop the current match. Does nothing if a match is already running
        /// </summary>
        void Stop();

        MatchState GetState();

        Task SetPlayer(int index, string charName, string? playerName);

        void SetScore(int index, int score);

        int GetScore(int index);

        TrackedPlayer? GetPlayer(int index);

        List<TrackedPlayer> GetPlayers();

        /// <summary>
        /// Get the <c>DateTime</c> of when a match was started
        /// </summary>
        DateTime GetMatchStart();

        /// <summary>
        /// Get the <c>DateTime</c> of when the match ended, or <c>null</c> if it hasn't ended
        /// </summary>
        DateTime? GetMatchEnd();

        int GetMatchLength();

    }

}
