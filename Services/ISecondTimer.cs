using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models.Events;

namespace watchtower.Services {

    /// <summary>
    /// Global timer that ticks every second
    /// </summary>
    public interface ISecondTimer {

        event EventHandler<SecondTimerArgs> OnTick;

    }

}
