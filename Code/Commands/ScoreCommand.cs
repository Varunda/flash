using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models;
using watchtower.Models.Events;
using watchtower.Services;

namespace watchtower.Commands {

    [Command]
    public class ScoreCommand {

        private readonly ILogger<ScoreCommand> _Logger;
        private readonly MatchManager _Match;

        public ScoreCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<ScoreCommand>>();
            _Match = services.GetRequiredService<MatchManager>();
        }

        public void Add(int index, int amount) {
            TrackedPlayer? player = _Match.GetPlayer(index);
            if (player == null) {
                _Logger.LogWarning($"Player {index} does not exist, got null from manager");
                return;
            }
            _Logger.LogInformation($"Setting score of runner {index}:{player.RunnerName} to {player.Score + amount}");

            _Match.SetScore(index, player.Score + amount);
        }

        public void Kill(int index) {
            TrackedPlayer? player = _Match.GetPlayer(0);
            if (player == null) {
                _Logger.LogWarning($"Runner {index} does not exist, got null from manager");
                return;
            }

            KillEvent ev = new KillEvent();
            ev.Timestamp = DateTime.UtcNow;

            player.ValidKills.Add(ev);

            _Match.SetScore(index, player.Score + 1);
        }

    }
}
