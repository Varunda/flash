﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Census;
using watchtower.Models;
using watchtower.Services;

namespace watchtower.Commands {

    [Command]
    public class PingCommand {

        private readonly ILogger<PingCommand> _Logger;
        private readonly ICharacterCollection _Characters;

        public PingCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<PingCommand>>();

            _Characters = services.GetRequiredService<ICharacterCollection>();
        }

        public void Ping() {
            _Logger.LogInformation($"Pong");
            Console.WriteLine($"Pong");
        }

        public void TestAdd(int i) {
            Console.WriteLine($"{i + 5}");
        }

        public async Task Load(string charID) {
            Character? c = await _Characters.GetByIDAsync(charID);
            if (c != null) {
                _Logger.LogInformation($"Loaded {c.Name}");
            } else {
                _Logger.LogInformation($"Failed to find {charID}");
            }
        }

    }
}
