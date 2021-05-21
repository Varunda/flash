using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace watchtower.Models.Events {

    public class SecondTimerArgs {

        /// <summary>
        /// How many seconds in total have elapsed since the start of the timer
        /// </summary>
        public int Seconds { get; set; }

        /// <summary>
        /// <c>DateTime</c> when the timer started
        /// </summary>
        public DateTime Start { get; set; }

        /// <summary>
        /// How many ticks in total have elapsed since the start of the timer
        /// </summary>
        public long TotalTicks { get; set; }

        /// <summary>
        /// How many ticks have occured since the last timer 
        /// </summary>
        public long ElapsedTicks { get; set; }

    }
}
