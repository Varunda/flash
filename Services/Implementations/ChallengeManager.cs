using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using watchtower.Code.Challenge;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Services.Implementations {

    public class ChallengeManager : IChallengeManager {

        private readonly IChallengeEventBroadcastService _ChallengeEvents;
        private readonly ITwitchChatBroadcastService _TwitchChat;

        private readonly ILogger<ChallengeManager> _Logger;

        private List<IndexedChallenge> _RunningChallenges = new List<IndexedChallenge>();
        private List<IRunChallenge> _AllChallenges = new List<IRunChallenge>();
        private List<IRunChallenge> _ActiveChallenges = new List<IRunChallenge>();

        private const double TICKS_PER_SECOND = 10000000D;
        private bool _IsPollRunning = false;
        private Timer _PollTimer = new Timer();
        private int _PollTimerLeft = 0;
        private DateTime _TimerLastTick = DateTime.UtcNow;
        private long _PollTotalTicks = 0;
        private ChallengePollOptions? _PollOptions;
        private ChallengePollResults? _PollResults;

        public ChallengeManager(ILogger<ChallengeManager> logger,
            IChallengeEventBroadcastService challengeEvents, ITwitchChatBroadcastService twitchChat) {

            _ChallengeEvents = challengeEvents ?? throw new ArgumentNullException(nameof(challengeEvents));
            _TwitchChat = twitchChat ?? throw new ArgumentNullException(nameof(twitchChat));

            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _AllChallenges.Add(new HeadshotChallenge());
            _AllChallenges.Add(new MedicChallenge());

            _Logger.LogInformation($"Loaded challenges:\n{String.Join("\n", _AllChallenges.Select(iter => $"\t{iter.ID}/{iter.Name}: {iter.Description}"))}");

            _TwitchChat.OnChatMessage += OnTwitchChat;

            _PollTimer.Interval = 1000;
            _PollTimer.AutoReset = true;
            _PollTimer.Elapsed += OnTimerTick;
        }

        public List<IRunChallenge> GetActive() => new List<IRunChallenge>(_ActiveChallenges);
        public List<IRunChallenge> GetAll() => new List<IRunChallenge>(_AllChallenges);
        public List<IndexedChallenge> GetRunning() => new List<IndexedChallenge>(_RunningChallenges);

        private void OnTwitchChat(object? sender, Ps2EventArgs<TwitchChatMessage> args) {
            if (_IsPollRunning == false) {
                return;
            }

            if (_PollResults == null) {
                _Logger.LogWarning($"_PollResults is null, while _IsPollRunning is true and during OnTwitchChat");
                return;
            }

            TwitchChatMessage msg = args.Payload;

            if (Int32.TryParse(msg.Message, out int voteOption) == true) {
                foreach (KeyValuePair<int, ChallengePollResult> entry in _PollResults.Options) {
                    if (entry.Value.Users.Contains(msg.Username)) {
                        entry.Value.Users.Remove(msg.Username);
                    }
                }

                if (_PollResults.Options.TryGetValue(voteOption, out ChallengePollResult? result) == true) {
                    result.Users.Add(msg.Username);
                    _Logger.LogTrace($"{msg.Username} voted for option {voteOption}");
                } else {
                    _Logger.LogTrace($"{msg.Username} used invalid option {voteOption}");
                }
            }
        }

        public void StartPoll(ChallengePollOptions options) {
            if (_IsPollRunning == true) {
                _Logger.LogWarning($"Not starting new poll: already running");
                return;
            }

            if (options.Possible.Count <= 0) {
                _Logger.LogWarning($"Not starting new poll: no options given");
                return;
            }

            if (options.VoteTime <= 0) {
                _Logger.LogWarning($"Not starting new poll: vote time is equal to or less than 0");
                return;
            }

            _PollTimerLeft = options.VoteTime;
            _TimerLastTick = DateTime.UtcNow;
            _IsPollRunning = true;
            _PollOptions = options;
            _PollTotalTicks = 0;
            _PollResults = new ChallengePollResults();

            int index = 1;
            foreach (int challengeID in options.Possible) {
                IRunChallenge? challenge = _AllChallenges.FirstOrDefault(iter => iter.ID == challengeID);
                if (challenge == null) {
                    _Logger.LogWarning($"Failed to find challenge {challengeID}");
                    continue;
                }

                _PollResults.Options.Add(index++, new ChallengePollResult(challenge) {
                    ChallengeID = challengeID
                });
                _Logger.LogTrace($"Added challenge {challenge.ID}/{challenge.Name} to poll at index {index - 1}");
            }

            _ChallengeEvents.EmitChallengePollStarted(_PollResults);
            _ChallengeEvents.EmitChallengePollTimerUpdate(_PollTimerLeft);

            _PollTimer.Start();

            _Logger.LogDebug($"Starting new poll, will finish after {options.VoteTime}");
        }

        public void EndPoll() {
            _PollTimer.Stop();
            _Logger.LogInformation($"Poll timer ended!");
            _IsPollRunning = false;

            if (_PollResults == null) {
                _Logger.LogWarning($"_PollResults is null in OnTimerTick, cannot emit ended event");
            } else {
                string msg = $"Poll results: ({_PollResults.Options.Count})\n";
                foreach (KeyValuePair<int, ChallengePollResult> entry in _PollResults.Options) {
                    msg += $"\t{entry.Value.Challenge.Name}> {String.Join(", ", entry.Value.Users)}\n";
                }

                int count = 0;
                List<ChallengePollResult> possibleOptions = new List<ChallengePollResult>();
                foreach (KeyValuePair<int, ChallengePollResult> entry in _PollResults.Options) {
                    if (entry.Value.Users.Count > count) {
                        possibleOptions.Clear();
                        count = entry.Value.Users.Count;
                        possibleOptions.Add(entry.Value);
                    } else if (entry.Value.Users.Count == count) {
                        possibleOptions.Add(entry.Value);
                    }
                }

                int index = new Random().Next(possibleOptions.Count);
                _PollResults.WinnerChallengeID = possibleOptions[index].ChallengeID;

                msg += $"\tWinner challenge ID: {_PollResults.WinnerChallengeID}";

                _Logger.LogDebug(msg);

                _ChallengeEvents.EmitChallengePollResultsEnded(_PollResults);
                Start(_PollResults.WinnerChallengeID.Value);

                _PollResults = null;
            }
        }

        public ChallengePollResults? GetPollResults() => _PollResults;

        public int GetPollTimer() => _PollTimerLeft;

        private void OnTimerTick(object? sender, ElapsedEventArgs args) {
            DateTime time = args.SignalTime.ToUniversalTime();

            long nowTicks = time.Ticks;
            long prevTicks = _TimerLastTick.Ticks;

            _PollTotalTicks += nowTicks - prevTicks;
            _TimerLastTick = time;

            if (_PollOptions != null) {
                _PollTimerLeft = _PollOptions.VoteTime - ((int)Math.Round(_PollTotalTicks / TICKS_PER_SECOND));
                _Logger.LogTrace($"Time left on current poll: {_PollTimerLeft}");
            }

            _ChallengeEvents.EmitChallengePollTimerUpdate(_PollTimerLeft);

            if (_PollTimerLeft <= 0) {
                EndPoll();
            } else {
                if (_PollResults == null) {
                    _Logger.LogWarning($"_PollResults is null in OnTimerTick, cannot emit update event");
                } else {
                    _ChallengeEvents.EmitChallengePollResultsUpdate(_PollResults);
                }
            }
        }

        public void Add(int ID) {
            IRunChallenge? newChall = _AllChallenges.FirstOrDefault(iter => iter.ID == ID);
            if (newChall == null) {
                _Logger.LogWarning($"Cannot add active challenge {ID}, does not exist");
                return;
            }

            IRunChallenge? chall = _ActiveChallenges.FirstOrDefault(iter => iter.ID == ID);
            if (chall != null) {
                _Logger.LogDebug($"Not adding already active challenge {chall.ID}/{chall.Name}");
                return;
            }

            _ActiveChallenges.Add(newChall);
            _Logger.LogInformation($"Added active challenge {newChall.ID}/{newChall.Name}");
        }

        public void Remove(int ID) {
            IRunChallenge? newChall = _AllChallenges.FirstOrDefault(iter => iter.ID == ID);
            if (newChall == null) {
                _Logger.LogWarning($"Cannot remove active challenge {ID}, does not exist");
                return;
            }

            IRunChallenge? chall = _ActiveChallenges.FirstOrDefault(iter => iter.ID == ID);
            if (chall == null) {
                _Logger.LogDebug($"Challenge {newChall.ID}/{newChall.Name} is not active, not removing");
                return;
            }

            _ActiveChallenges = _ActiveChallenges.Where(iter => iter.ID != ID).ToList();
            _Logger.LogInformation($"Removed active challenge {chall.ID}/{chall.Name}");
        }

        public void Start(int ID) {
            IRunChallenge? chall = _ActiveChallenges.FirstOrDefault(iter => iter.ID == ID);
            if (chall == null) {
                _Logger.LogWarning($"Cannot start challenge {ID}, is not active, or does not exist");
                return;
            }

            IndexedChallenge? running = _RunningChallenges.FirstOrDefault(iter => iter.Challenge.ID == ID);
            if (running != null) {
                _Logger.LogWarning($"Not adding repeat challenge {chall.ID}/{chall.Name}");
                return;
            }

            IndexedChallenge newChall = new IndexedChallenge(chall);
            _RunningChallenges.Add(newChall);
            _Logger.LogInformation($"Started new challenge {newChall.Challenge.ID}/{newChall.Challenge.Name}, index {newChall.Index}");
            _ChallengeEvents.EmitChallengeStart(newChall);
        }

        public void End(int index) {
            IndexedChallenge? running = _RunningChallenges.FirstOrDefault(iter => iter.Index == index);
            if (running == null) {
                _Logger.LogWarning($"Cannot end running challenge index {index}, index not found");
                return;
            }

            _ChallengeEvents.EmitChallengeEnded(running);
            _RunningChallenges = _RunningChallenges.Where(iter => iter.Index != index).ToList();
            _Logger.LogInformation($"Ended running challenge {running.Challenge.ID}/{running.Challenge.Name}, index {index}");
        }

    }
}
