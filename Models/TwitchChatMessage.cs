using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace watchtower.Models {

    public class TwitchChatMessage {

        public string ID { get; set; } = "";

        public string Channel { get; set; } = "";

        public string Username { get; set; } = "";

        public string UserID { get; set; } = "";

        public string Message { get; set; } = "";

    }
}
