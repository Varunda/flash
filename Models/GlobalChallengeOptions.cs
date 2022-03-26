using watchtower.Code.Challenge;

namespace watchtower.Models {

    /// <summary>
    /// Global challenge options about how the duration of them will be treated
    /// </summary>
    public class GlobalChallengeOptions {

        public ChallengeDurationType Type { get; set; } = ChallengeDurationType.KILLS;

        public int Duration { get; set; } = 10;

    }
}
