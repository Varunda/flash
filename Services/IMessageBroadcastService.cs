using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Services {

    public interface IMessageBroadcastService {

        event EventHandler<Ps2EventArgs<string>>? OnMessageEvent;

        event EventHandler<Ps2EventArgs<int>>? OnClearEvent; 

        void Log(string msg);

        void Clear();

        List<Message> GetMessages();

    }
}
