using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models.Events;

namespace watchtower.Models {

    public class TrackedPlayer {

        public string ID { get; set; } = "";

        public int Index { get; set; }

        public string CharacterName { get; set; } = "";

        public string RunnerName { get; set; } = "";

        public int Score { get; set; } = 0;

        public Character? Character { get; set; } = null;

        public List<KillEvent> Kills { get; set; } = new List<KillEvent>();

        public List<KillEvent> ValidKills { get; set; } = new List<KillEvent>();

        public List<KillEvent> Deaths { get; set; } = new List<KillEvent>();

        public List<ExpEvent> Exp { get; set; } = new List<ExpEvent>();

        public int Streak { get; set; } = 0;
        
        public List<int> Streaks { get; set; } = new List<int>();


    }
}
