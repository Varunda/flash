using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Code.Challenge;
using watchtower.Models;
using watchtower.Services;

namespace watchtower.Commands {

    [Command]
    public class ChallengeCommand {

        private readonly ILogger<ChallengeCommand> _Logger;

        private readonly IChallengeManager _Challenges;

        public ChallengeCommand(IServiceProvider services) {

            _Logger = services.GetRequiredService<ILogger<ChallengeCommand>>();
            _Challenges = services.GetRequiredService<IChallengeManager>();
        }

        public void All() {
            List<IRunChallenge> challs = _Challenges.GetAll();

            _Logger.LogInformation($"All challenges:\n{String.Join("\n", challs.Select(iter => $"\t{iter.ID}/{iter.Name}: {iter.Description}"))}");
        }

        public void Active() {
            List<IRunChallenge> challs = _Challenges.GetActive();

            _Logger.LogInformation($"Active challenges:\n{String.Join("\n", challs.Select(iter => $"\t{iter.ID}/{iter.Name}: {iter.Description}"))}");
        }

        public void Running() {
            List<IndexedChallenge> challs = _Challenges.GetRunning();

            _Logger.LogInformation($"Active challenges:\n{String.Join("\n", challs.Select(iter => $"\t{iter.Index}@ {iter.Challenge.ID}/{iter.Challenge.Name}: {iter.Challenge.Description}"))}");
        }

        public void Add(int ID) {
            _Challenges.Add(ID);
        }

        public void Remove(int ID) {
            _Challenges.Remove(ID);
        }

        public void Start(int ID) {
            _Challenges.Start(ID);
        }

        public void End(int index) {
            _Challenges.End(index);
        }

        public void Poll() {
            _Challenges.Add(1);
            _Challenges.Add(2);

            _Challenges.StartPoll(new ChallengePollOptions() {
                Possible = new List<int>() { 1, 2 },
                VoteTime = 10
            });
        }

    }
}
