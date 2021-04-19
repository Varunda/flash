using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models.Events;

namespace watchtower.Services {

    /// <summary>
    /// Message broadcast for admin messages, usually in response to an action being done on the setup page
    /// </summary>
    public interface IAdminMessageBroadcastService : IMessageBroadcastService { }

}
