using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Services {

    /// <summary>
    /// Message broadcast for match events, such as a team getting a kill
    /// </summary>
    public interface IMatchMessageBroadcastService : IMessageBroadcastService { }

}
