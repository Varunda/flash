using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Commands;
using watchtower.Services;

namespace watchtower.Code.Commands {

    [Command]
    public class PollCommand {

        private readonly ILogger<PollCommand> _Logger;

        private readonly IChallengeManager _Challenges;

        public PollCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<PollCommand>>();
            _Challenges = services.GetRequiredService<IChallengeManager>();
        }

        public void Stop() {
            _Challenges.EndPoll();
        }

    }
}
