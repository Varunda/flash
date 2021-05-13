using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Code.Challenge;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Services.Implementations {

    public class ChallengeEventBroadcastService : IChallengeEventBroadcastService {

        public event EventHandler<Ps2EventArgs<ChallengePollResults>>? OnChallengePollStartedEvent;
        public void EmitChallengePollStarted(ChallengePollResults options) {
            OnChallengePollStartedEvent?.Invoke(this, new Ps2EventArgs<ChallengePollResults>(options));
        }

        public event EventHandler<Ps2EventArgs<IndexedChallenge>>? OnChallengeStartEvent;
        public void EmitChallengeStart(IndexedChallenge challenge) {
            OnChallengeStartEvent?.Invoke(this, new Ps2EventArgs<IndexedChallenge>(challenge));
        }

        public event EventHandler<Ps2EventArgs<IndexedChallenge>>? OnChallengeEndedEvent;
        public void EmitChallengeEnded(IndexedChallenge challenge) {
            OnChallengeEndedEvent?.Invoke(this, new Ps2EventArgs<IndexedChallenge>(challenge));
        }

        public event EventHandler<Ps2EventArgs<ChallengePollResults>>? OnChallengeResultsUpdateEvent;
        public void EmitChallengePollResultsUpdate(ChallengePollResults results) {
            OnChallengeResultsUpdateEvent?.Invoke(this, new Ps2EventArgs<ChallengePollResults>(results));
        }

        public event EventHandler<Ps2EventArgs<ChallengePollResults>>? OnChallengeResultsEndedEvent;
        public void EmitChallengePollResultsEnded(ChallengePollResults results) {
            OnChallengeResultsEndedEvent?.Invoke(this, new Ps2EventArgs<ChallengePollResults>(results));
        }

        public event EventHandler<Ps2EventArgs<int>>? OnChallengePollTimerUpdateEvent;
        public void EmitChallengePollTimerUpdate(int timeLeft) {
            OnChallengePollTimerUpdateEvent?.Invoke(this, new Ps2EventArgs<int>(timeLeft));
        }

        public event EventHandler<Ps2EventArgs<IndexedChallenge>>? OnChallengeUpdateEvent;
        public void EmitChallengeUpdate(IndexedChallenge challenge) {
            OnChallengeUpdateEvent?.Invoke(this, new Ps2EventArgs<IndexedChallenge>(challenge));
        }

    }
}
