using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace watchtower.Models.Events {

    public class Ps2EventArgs<T>: EventArgs {

        public Ps2EventArgs(T m) {
            Payload = m;
            CreatedTime = DateTime.UtcNow;
        }

        public T Payload { get; set; }
        public DateTime CreatedTime { get; }

    }
}
