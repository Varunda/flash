using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Services;

namespace watchtower.Commands {

    [Command]
    public class LogCommand {

        private IAdminMessageBroadcastService _AdminMessages;
        private IMatchMessageBroadcastService _MatchMessages;

        private ILogger<LogCommand> _Logger;

        public LogCommand(IServiceProvider services) {
            _AdminMessages = services.GetRequiredService<IAdminMessageBroadcastService>();
            _MatchMessages = services.GetRequiredService<IMatchMessageBroadcastService>();

            _Logger = services.GetRequiredService<ILogger<LogCommand>>();
        }

        public void Clear(string which) {
            if (which.ToLower() == "admin") {
                _AdminMessages.Clear();
                _Logger.LogInformation($"Cleared admin logs");
            } else if (which.ToLower() == "match") {
                _MatchMessages.Clear();
                _Logger.LogInformation($"Cleared match logs");
            } else {
                _Logger.LogError($"Unknown message broadcast server '{which}'");
            }
        }

    }
}
