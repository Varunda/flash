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

        public event EventHandler<Ps2EventArgs<int>>? OnClearEvent;
        public delegate void ClearHandler(object? sender);

        public void Log(string msg) {
            lock (_Messages) {
                _Messages.Insert(0, new Message() {
                    Timestamp = DateTime.UtcNow,
                    Content = msg
                });
            }
            OnMessageEvent?.Invoke(this, new Ps2EventArgs<string>(msg));
        }

        public void Clear() {
            lock (_Messages) {
                _Messages.Clear();
            }
            OnClearEvent?.Invoke(this, new Ps2EventArgs<int>(0));
        }

        public List<Message> GetMessages() {
            lock (_Messages) {
                return new List<Message>(_Messages);
            }
        }

    }
}
