using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace watchtower.Models {

    public class RunnerStats {

        public string CharID { get; set; } = "";

        public string Name { get; set; } = "";

        public int Kills { get; set; }

        public int Deaths { get; set; }

        public int AvgStreak { get; set; }

        public int AssistCount { get; set; }

        public int TRKills { get; set; }

        public int NCKills { get; set; }

        public int VSKills { get; set; }

    }
}
