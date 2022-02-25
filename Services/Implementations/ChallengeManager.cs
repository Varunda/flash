using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using watchtower.Code;
using watchtower.Code.Challenge;
using watchtower.Models;
using watchtower.Models.Events;
using watchtower.Services.Queue;

namespace watchtower.Services.Implementations {

    public class ChallengeManager : IChallengeManager {

        private readonly ILogger<ChallengeManager> _Logger;

        private readonly IChallengeEventBroadcastService _ChallengeEvents;
        private readonly ITwitchChatBroadcastService _TwitchChat;
        private readonly IMatchMessageBroadcastService _MatchLog;
        private readonly IAdminMessageBroadcastService _AdminLog;
        private readonly DiscordThreadManager _ThreadManager;

        private ChallengeMode _Mode = ChallengeMode.NICE;

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
                IChallengeEventBroadcastService challengeEvents, ITwitchChatBroadcastService twitchChat,
                IMatchMessageBroadcastService matchLog, DiscordThreadManager threadManager,
                IAdminMessageBroadcastService adminLog) {

            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _ChallengeEvents = challengeEvents ?? throw new ArgumentNullException(nameof(challengeEvents));
            _TwitchChat = twitchChat ?? throw new ArgumentNullException(nameof(twitchChat));
            _MatchLog = matchLog;

            LoadChallenges();
            _Logger.LogInformation($"Loaded challenges:\n{string.Join("\n", _AllChallenges.Select(iter => $"\t{iter.ID}/{iter.Name}: {iter.Description}"))}");

            _TwitchChat.OnChatMessage += OnTwitchChat;

            _PollTimer.Interval = 1000;
            _PollTimer.AutoReset = true;
            _PollTimer.Elapsed += OnTimerTick;
            _ThreadManager = threadManager;
            _AdminLog = adminLog;
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

            _MatchLog.Log($"Started new challenge {newChall.Challenge.ID}/{newChall.Challenge.Name}, index {newChall.Index}");
            try {
                _ = _ThreadManager.SendThreadMessage($"New challenge started!\n{newChall.Challenge.Name}: {newChall.Challenge.Description}");
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to send message about challenge starting");
            }

            _ChallengeEvents.EmitChallengeStart(newChall);

            _ = _ThreadManager.PlayStartNoise();
        }

        public void End(int index) {
            IndexedChallenge? running = _RunningChallenges.FirstOrDefault(iter => iter.Index == index);
            if (running == null) {
                _Logger.LogWarning($"Cannot end running challenge index {index}, index not found");
                return;
            }

            _RunningChallenges = _RunningChallenges.Where(iter => iter.Index != index).ToList();

            string s = $"Ended running challenge {running.Challenge.ID}/{running.Challenge.Name}";
            _Logger.LogInformation(s);
            _MatchLog.Log(s);
            _ = _ThreadManager.SendThreadMessage(s);
            _ = _ThreadManager.PlayEndNoise();

            _ChallengeEvents.EmitChallengeEnded(running);
        }

        public void SetMode(ChallengeMode mode) {
            _Mode = mode;

            _Logger.LogInformation($"Challenge mode set to {mode}");
            _AdminLog.Log($"Challenge mode set to {mode}");

            _ChallengeEvents.EmitModeChange(mode);
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

            _ChallengeEvents.EmitPollStarted(_PollResults);
            _ChallengeEvents.EmitPollTimerUpdate(_PollTimerLeft);

            _PollTimer.Start();

            _Logger.LogDebug($"Starting new poll, will finish after {options.VoteTime}");
        }

        public void EndPoll() {
            if (_IsPollRunning == false) {
                _Logger.LogDebug($"Cannot end poll, one was not running");
                return;
            }

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

                _ChallengeEvents.EmitPollEnded(_PollResults);
                Start(_PollResults.WinnerChallengeID.Value);

                _PollResults = null;
            }
        }

        public void AddActive(int ID) {
            IRunChallenge? challenge = _AllChallenges.FirstOrDefault(iter => iter.ID == ID);
            if (challenge == null) {
                _Logger.LogWarning($"Cannot add active challenge {ID}: Challenge {ID} doesn't exist");
                return;
            }

            if (_ActiveChallenges.FirstOrDefault(iter => iter.ID == ID) != null) {
                _Logger.LogWarning($"Cannot add active challenge {ID}: Challenge {ID}/{challenge.Name} is already active");
                return;
            }

            _ActiveChallenges.Add(challenge);

            _ChallengeEvents.EmitActiveListUpdate(_ActiveChallenges);
        }

        public void RemoveActive(int ID) {
            IRunChallenge? challenge = _AllChallenges.FirstOrDefault(iter => iter.ID == ID);
            if (challenge == null) {
                _Logger.LogWarning($"Cannot remove active challenge {ID}: Challenge {ID} doesn't exist");
                return;
            }

            if (_ActiveChallenges.FirstOrDefault(iter => iter.ID == ID) == null) {
                _Logger.LogWarning($"Cannot remove active challenge {ID}: Challenge {ID}/{challenge.Name} is not active");
                return;
            }

            _ActiveChallenges = _ActiveChallenges.Where(iter => {
                return iter.ID != ID;
            }).ToList();

            _ChallengeEvents.EmitActiveListUpdate(_ActiveChallenges);
        }

        private void OnTimerTick(object? sender, ElapsedEventArgs args) {
            DateTime time = args.SignalTime.ToUniversalTime();

            long nowTicks = time.Ticks;
            long prevTicks = _TimerLastTick.Ticks;

            _PollTotalTicks += nowTicks - prevTicks;
            _TimerLastTick = time;

            if (_PollOptions != null) {
                _PollTimerLeft = _PollOptions.VoteTime - ((int)Math.Round(_PollTotalTicks / TICKS_PER_SECOND));
                //_Logger.LogTrace($"Time left on current poll: {_PollTimerLeft}");
            }

            _ChallengeEvents.EmitPollTimerUpdate(_PollTimerLeft);

            if (_PollTimerLeft <= 0) {
                EndPoll();
            } else {
                if (_PollResults == null) {
                    _Logger.LogWarning($"_PollResults is null in OnTimerTick, cannot emit update event");
                } else {
                    _ChallengeEvents.EmitPollResultsUpdate(_PollResults);
                }
            }
        }

        private void LoadChallenges() {
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes()) {
                if (typeof(IRunChallenge).IsAssignableFrom(type) == true && type.IsInterface == false) {
                    _Logger.LogTrace($"Found challenge {type.Name}");

                    try {
                        object? clazz = Activator.CreateInstance(type);
                        if (clazz != null) {
                            IRunChallenge? challenge = clazz as IRunChallenge;
                            if (challenge != null) {
                                _AllChallenges.Add(challenge);
                                _Logger.LogDebug($"Successfully created challenge {type.Name} as {challenge.ID}/{challenge.Name}");
                            } else {
                                _Logger.LogWarning($"type isn't an IRunChallenge");
                            }
                        } else {
                            _Logger.LogWarning($"clazz is null");
                        }
                    } catch (Exception ex) {
                        _Logger.LogWarning($"Failed to create challenge {type.Name}: {ex.Message}");
                    }
                }
            }

            _AllChallenges = _AllChallenges.OrderBy(iter => iter.ID).ToList();
        }

        private void OnTwitchChat(object? sender, Ps2EventArgs<TwitchChatMessage> args) {
            if (_IsPollRunning == false) {
                return;
            }

            if (_PollResults == null) {
                _Logger.LogWarning($"_PollResults is null, while _IsPollRunning is true and during OnTwitchChat");
                return;
            }

            TwitchChatMessage msg = args.Payload;

            if (int.TryParse(msg.Message, out int voteOption) == true) {
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

        public ChallengePollResults? GetPollResults() => _PollResults;
        public int GetPollTimer() => _PollTimerLeft;
        public ChallengeMode GetMode() => _Mode;
        public List<IRunChallenge> GetActive() => new List<IRunChallenge>(_ActiveChallenges);
        public List<IRunChallenge> GetAll() => new List<IRunChallenge>(_AllChallenges);
        public List<IndexedChallenge> GetRunning() => new List<IndexedChallenge>(_RunningChallenges);

    }
}
