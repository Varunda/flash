using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Services {

    public class AdminMessageBroadcastService : IAdminMessageBroadcastService {

        private List<Message> _Messages = new List<Message>();

        public event EventHandler<Ps2EventArgs<string>>? OnMessageEvent;
        public delegate void MessageHandler(object? sender, string msg);

        public void EmitMessage(string msg) {
            _Messages.Insert(0, new Message() {
                Timestamp = DateTime.UtcNow,
                Content = msg
            });
            OnMessageEvent?.Invoke(this, new Ps2EventArgs<string>(msg));
        }

        public List<Message> GetMessages() => new List<Message>(_Messages);

    }
}
