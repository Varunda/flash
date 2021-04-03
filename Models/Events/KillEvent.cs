using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace watchtower.Models.Events {

    public class KillEvent {

        public string SourceID { get; set; } = "";

        public DateTime Timestamp { get; set; }

        public string ZoneID { get; set; } = "";

        public string LoadoutID { get; set; } = "";

        public string TargetID { get; set; } = "";

        public string TargetLoadoutID { get; set; } = "";

        public string WeaponID { get; set; } = "";

        public bool IsHeadshot { get; set; }

        public string WorldID { get; set; } = "";

    }

}
