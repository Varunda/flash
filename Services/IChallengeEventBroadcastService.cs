using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Code.Challenge;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Services {

    public interface IChallengeEventBroadcastService {

        event EventHandler<Ps2EventArgs<ChallengePollResults>> OnChallengePollStartedEvent;
        void EmitChallengePollStarted(ChallengePollResults options);

        event EventHandler<Ps2EventArgs<ChallengePollResults>> OnChallengeResultsUpdateEvent;
        void EmitChallengePollResultsUpdate(ChallengePollResults results);

        event EventHandler<Ps2EventArgs<ChallengePollResults>> OnChallengeResultsEndedEvent;
        void EmitChallengePollResultsEnded(ChallengePollResults results);

        event EventHandler<Ps2EventArgs<IndexedChallenge>> OnChallengeStartEvent;
        void EmitChallengeStart(IndexedChallenge challenge);

        event EventHandler<Ps2EventArgs<int>> OnChallengePollTimerUpdateEvent;
        void EmitChallengePollTimerUpdate(int timeLeft);

        event EventHandler<Ps2EventArgs<IndexedChallenge>> OnChallengeEndedEvent;
        void EmitChallengeEnded(IndexedChallenge challenge);

    }
}
