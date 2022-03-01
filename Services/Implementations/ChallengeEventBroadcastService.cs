using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Code;
using watchtower.Code.Challenge;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Services.Implementations {

    public class ChallengeEventBroadcastService : IChallengeEventBroadcastService {

        public event EventHandler<Ps2EventArgs<ChallengePollResults>>? OnPollStarted;
        public void EmitPollStarted(ChallengePollResults options) {
            OnPollStarted?.Invoke(this, new Ps2EventArgs<ChallengePollResults>(options));
        }

        public event EventHandler<Ps2EventArgs<ChallengePollResults>>? OnPollResultsUpdate;
        public void EmitPollResultsUpdate(ChallengePollResults results) {
            OnPollResultsUpdate?.Invoke(this, new Ps2EventArgs<ChallengePollResults>(results));
        }

        public event EventHandler<Ps2EventArgs<ChallengePollResults>>? OnPollEnded;
        public void EmitPollEnded(ChallengePollResults results) {
            OnPollEnded?.Invoke(this, new Ps2EventArgs<ChallengePollResults>(results));
        }

        public event EventHandler<Ps2EventArgs<int>>? OnPollTimerUpdate;
        public void EmitPollTimerUpdate(int timeLeft) {
            OnPollTimerUpdate?.Invoke(this, new Ps2EventArgs<int>(timeLeft));
        }

        public event EventHandler<Ps2EventArgs<IndexedChallenge>>? OnChallengeStart;
        public void EmitChallengeStart(IndexedChallenge challenge) {
            OnChallengeStart?.Invoke(this, new Ps2EventArgs<IndexedChallenge>(challenge));
        }

        public event EventHandler<Ps2EventArgs<IndexedChallenge>>? OnChallengeUpdate;
        public void EmitChallengeUpdate(IndexedChallenge challenge) {
            OnChallengeUpdate?.Invoke(this, new Ps2EventArgs<IndexedChallenge>(challenge));
        }

        public event EventHandler<Ps2EventArgs<IndexedChallenge>>? OnChallengeEnded;
        public void EmitChallengeEnded(IndexedChallenge challenge) {
            OnChallengeEnded?.Invoke(this, new Ps2EventArgs<IndexedChallenge>(challenge));
        }

        public event EventHandler<Ps2EventArgs<ChallengeMode>>? OnModeChange;
        public void EmitModeChange(ChallengeMode mode) {
            OnModeChange?.Invoke(this, new Ps2EventArgs<ChallengeMode>(mode));
        }

        public event EventHandler<Ps2EventArgs<List<IRunChallenge>>>? OnActiveListUpdate;
        public void EmitActiveListUpdate(List<IRunChallenge> challenges) {
            OnActiveListUpdate?.Invoke(this, new Ps2EventArgs<List<IRunChallenge>>(new List<IRunChallenge>(challenges)));
        }

        public event EventHandler<Ps2EventArgs<GlobalChallengeOptions>>? OnGlobalOptionsUpdate;
        public void EmitGlobalOptionsUpdate(GlobalChallengeOptions options) {
            OnGlobalOptionsUpdate?.Invoke(this, new Ps2EventArgs<GlobalChallengeOptions>(options));
        }


    }
}
