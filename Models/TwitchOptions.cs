using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace watchtower.Models {

    public class TwitchOptions {

        public string AccessToken { get; set; } = "";

        public string ChatUsername { get; set; } = "";

        public string ChatOAuth { get; set; } = "";

        public string ChatTargetChannel { get; set; } = "";

        public string RefreshToken { get; set; } = "";

        public string ClientID { get; set; } = "";

    }
}
