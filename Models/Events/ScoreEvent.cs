using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace watchtower.Models.Events {

    public class ScoreEvent {

        public int ScoreChange { get; set; }

        public int TotalScore { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    }
}
