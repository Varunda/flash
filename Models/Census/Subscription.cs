using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace watchtower.Models.Census {

    public class Subscription {

        public List<string> Characters { get; set; } = new List<string>();

        public List<string> Events { get; set; } = new List<string>();

        public List<string> Worlds { get; set; } = new List<string>();

    }

}
