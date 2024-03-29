﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models.Events;

namespace watchtower.Models {

    /// <summary>
    /// 
    /// </summary>
    public class TrackedPlayer {

        public int Index { get; set; } = 0;

        public string RunnerName { get; set; } = "";

        public int Score { get; set; } = 0;

        public List<Character> Characters { get; set; } = new List<Character>();

        public List<KillEvent> Kills { get; set; } = new List<KillEvent>();

        public List<KillEvent> ValidKills { get; set; } = new List<KillEvent>();

        public List<KillEvent> Deaths { get; set; } = new List<KillEvent>();

        public List<ExpEvent> Exp { get; set; } = new List<ExpEvent>();

        public List<ScoreEvent> Scores { get; set; } = new List<ScoreEvent>();

        public int Wins { get; set; } = 0;

        public int Streak { get; set; } = 0;
        
        public List<int> Streaks { get; set; } = new List<int>();

    }
}
