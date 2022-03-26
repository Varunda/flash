using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Code;
using watchtower.Code.Challenge;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Services {

    public interface IChallengeEventBroadcastService {

        event EventHandler<Ps2EventArgs<ChallengePollResults>> OnPollStarted;
        void EmitPollStarted(ChallengePollResults options);

        event EventHandler<Ps2EventArgs<ChallengePollResults>> OnPollResultsUpdate;
        void EmitPollResultsUpdate(ChallengePollResults results);

        event EventHandler<Ps2EventArgs<ChallengePollResults>> OnPollEnded;
        void EmitPollEnded(ChallengePollResults results);

        event EventHandler<Ps2EventArgs<int>> OnPollTimerUpdate;
        void EmitPollTimerUpdate(int timeLeft);

        event EventHandler<Ps2EventArgs<IndexedChallenge>> OnChallengeStart;
        void EmitChallengeStart(IndexedChallenge challenge);

        event EventHandler<Ps2EventArgs<IndexedChallenge>> OnChallengeEnded;
        void EmitChallengeEnded(IndexedChallenge challenge);

        event EventHandler<Ps2EventArgs<IndexedChallenge>> OnChallengeUpdate;
        void EmitChallengeUpdate(IndexedChallenge challenge);

        event EventHandler<Ps2EventArgs<ChallengeMode>> OnModeChange;
        void EmitModeChange(ChallengeMode mode);

        event EventHandler<Ps2EventArgs<List<IRunChallenge>>> OnActiveListUpdate;
        void EmitActiveListUpdate(List<IRunChallenge> challenges);

        event EventHandler<Ps2EventArgs<GlobalChallengeOptions>> OnGlobalOptionsUpdate;
        void EmitGlobalOptionsUpdate(GlobalChallengeOptions options);

    }
}
