using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using watchtower.Census;
using watchtower.Code;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Census;
using watchtower.Models.Events;
using watchtower.Realtime;

namespace watchtower.Services {

    public class MatchManager : IMatchManager {

        const double TICKS_PER_SECOND = 10000000D;

        private readonly ILogger<MatchManager> _Logger;

        private readonly ICharacterCollection _CharacterColleciton;
        private readonly IItemCollection _ItemCollection;

        private readonly IRealtimeMonitor _Realtime;
        private readonly IRealtimeEventBroadcastService _RealtimeEvents;
        private readonly IMatchEventBroadcastService _MatchEvents;
        private readonly IMatchMessageBroadcastService _MatchMessages;
        private readonly IAdminMessageBroadcastService _AdminMessages;
        private readonly IChallengeManager _Challenges;
        private readonly IChallengeEventBroadcastService _ChallengeEvents;
        private readonly ISecondTimer _Timer;

        private readonly Dictionary<int, TrackedPlayer> _Players = new Dictionary<int, TrackedPlayer>();
        private MatchState _State = MatchState.UNSTARTED;
        private DateTime _MatchStart = DateTime.UtcNow;
        private DateTime? _MatchEnd = null;
        private long _MatchTicks = 0;
        private MatchSettings _Settings = new MatchSettings();

        public MatchManager(ILogger<MatchManager> logger,
                ICharacterCollection charColl, IItemCollection itemColl,
                IRealtimeEventBroadcastService events, IMatchEventBroadcastService matchEvents,
                IRealtimeMonitor realtime, IChallengeEventBroadcastService challengeEvents,
                IMatchMessageBroadcastService matchMessages, IAdminMessageBroadcastService adminMessages,
                IChallengeManager challenges, ISecondTimer timer) {

            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _CharacterColleciton = charColl ?? throw new ArgumentNullException(nameof(charColl));
            _ItemCollection = itemColl ?? throw new ArgumentNullException(nameof(itemColl));

            _Realtime = realtime ?? throw new ArgumentNullException(nameof(realtime));
            _RealtimeEvents = events ?? throw new ArgumentNullException(nameof(events));
            _MatchEvents = matchEvents ?? throw new ArgumentNullException(nameof(matchEvents));
            _ChallengeEvents = challengeEvents ?? throw new ArgumentNullException(nameof(challengeEvents));

            _MatchMessages = matchMessages ?? throw new ArgumentNullException(nameof(matchMessages));
            _AdminMessages = adminMessages ?? throw new ArgumentNullException(nameof(adminMessages));
            _Challenges = challenges ?? throw new ArgumentNullException(nameof(challenges));

            _Timer = timer ?? throw new ArgumentNullException(nameof(timer));

            SetSettings(new MatchSettings());

            AddListeners();
        }

        private void AddListeners() {
            _RealtimeEvents.OnKillEvent += KillHandler;
            _RealtimeEvents.OnExpEvent += ExpHandler;

            _Timer.OnTick += OnTick;
        }

        public async Task<bool> AddCharacter(int index, string charName) {
            if (_Players.TryGetValue(index, out TrackedPlayer? player) == false) {
                player = new TrackedPlayer {
                    Index = index,
                    RunnerName = $"Runner {index + 1}"
                };

                _Players.Add(index, player);

                _AdminMessages.Log($"Created team {player.Index}:{player.RunnerName}");
            }

            foreach (Character c in player.Characters) {
                if (c.Name.ToLower() == charName.ToLower()) {
                    _Logger.LogWarning($"Not adding duplicate players {charName}");
                    return true;
                }
            }

            Character? ch = await _CharacterColleciton.GetByNameAsync(charName);
            if (ch == null) {
                _Logger.LogWarning($"Failed to add character {charName} to Runner {index}, does not exist");
                return false;
            }

            if (player.RunnerName == $"Runner {index + 1}") {
                _AdminMessages.Log($"Renamed team {index}:{player.RunnerName} to {ch.Name}");
                player.RunnerName = ch.Name;
            }

            player.Characters.Add(ch);

            _Realtime.Subscribe(new Subscription() {
                Characters = { ch.ID },
                Events = { 
                    "Death",
                    "GainExperience" 
                }
            });

            _AdminMessages.Log($"Team {index}:{player.RunnerName} added character {charName}");

            _MatchEvents.EmitPlayerUpdateEvent(index, player);

            return true;
        }

        public void RemoveCharacter(int index, string charName) {
            if (_Players.TryGetValue(index, out TrackedPlayer? player) == true) {
                player.Characters = player.Characters.Where(iter => iter.Name.ToLower() != charName.ToLower()).ToList();
                _AdminMessages.Log($"Team {index}:{player.RunnerName} removed character {charName}");
            } else {
                _Logger.LogWarning($"Cannot remove {charName} from player {index} cause it wasn't found");
            }
        }


        public void SetSettings(MatchSettings settings) {
            if (_State == MatchState.RUNNING) {
                _Logger.LogWarning($"Match is currently running, some settings may create funky behavior");
            }

            _Settings = settings;

            _Logger.LogInformation($"Match settings:" +
                $"\n\tKillGoal: {_Settings.KillGoal}"
            );

            _MatchEvents.EmitMatchSettingsEvent(_Settings);
        }

        public void SetRunnerName(int index, string? runnerName) {
            if (_Players.TryGetValue(index, out TrackedPlayer? player) == true) {
                player.RunnerName = runnerName ?? $"Runner {index + 1}";
            } else {
                _Logger.LogWarning($"Cannot set runner name for {index}, not in _Players");
            }
        }

        public void SetScore(int index, int score) {
            if (_Players.TryGetValue(index, out TrackedPlayer? player) == true) {
                player.Score = score;
                _MatchEvents.EmitPlayerUpdateEvent(player.Index, player);

                if (player.Score >= _Settings.KillGoal) {
                    _MatchMessages.Log($"Team {index}:{player.RunnerName} reached goal {_Settings.KillGoal}, ending match");
                    StopRound(index);
                }
            } else {
                _Logger.LogWarning($"Cannot set score of runner {index}, _Players does not contain");
            }
        }

        public int? GetScore(int index) {
            if (_Players.TryGetValue(index, out TrackedPlayer? player) == true) {
                return player.Score;
            }

            _Logger.LogWarning($"Cannot get score of runner {index}, _Players does not contain");
            return null;
        }

        public TrackedPlayer? GetPlayer(int index) {
            if (_Players.TryGetValue(index, out TrackedPlayer? player) == true) {
                return player;
            }
            return null;
        }

        private void OnTick(object? sender, SecondTimerArgs args) {
            if (_State != MatchState.RUNNING) {
                return;
            }

            _MatchTicks += args.ElapsedTicks;
            _Logger.LogTrace($"ElapsedTicks: {args.ElapsedTicks}");

            _MatchEvents.EmitTimerEvent((int)Math.Round(_MatchTicks / TICKS_PER_SECOND));
        }

        public void StartRound() {
            if (_State == MatchState.RUNNING) {
                _Logger.LogWarning($"Not starting match, already started");
                return;
            }

            if (_State == MatchState.UNSTARTED) {
                _MatchTicks = 0;
                _MatchStart = DateTime.UtcNow;
                _AdminMessages.Log($"Match unstarted, resetting ticks and start");
            }

            SetState(MatchState.RUNNING);

            _AdminMessages.Log($"Match started at {_MatchStart}");
        }

        public void ClearMatch() {
            foreach (TrackedPlayer player in GetPlayers()) {
                player.Score = 0;
                player.Kills = new List<KillEvent>();
                player.ValidKills = new List<KillEvent>();
                player.Deaths = new List<KillEvent>();
                player.Exp = new List<ExpEvent>();
                player.Streak = 0;
                player.Streaks = new List<int>();
                player.Characters = new List<Character>();
                player.Wins = 0;
            }

            _Players.Clear();

            _MatchTicks = 0;

            SetState(MatchState.UNSTARTED);
            _MatchEvents.EmitTimerEvent(0);

            _MatchStart = DateTime.UtcNow;
            _MatchEnd = null;

            _AdminMessages.Clear();
            _MatchMessages.Clear();

            _AdminMessages.Log($"Match cleared at {DateTime.UtcNow}");
        }

        public void RestartRound() {
            _MatchStart = DateTime.UtcNow;
            _MatchEnd = null;
            _MatchTicks = 0;

            _MatchEvents.EmitTimerEvent(0);

            foreach (TrackedPlayer player in GetPlayers()) {
                player.Score = 0;
                player.Kills = new List<KillEvent>();
                player.ValidKills = new List<KillEvent>();
                player.Deaths = new List<KillEvent>();
                player.Exp = new List<ExpEvent>();
                player.Streak = 0;
                player.Streaks = new List<int>();
            }

            SetState(MatchState.UNSTARTED);

            _AdminMessages.Log($"Match restarted at {DateTime.UtcNow}");
        }

        public void PauseRound() {
            SetState(MatchState.PAUSED);

            _AdminMessages.Log($"Round paused at {DateTime.UtcNow}");
        }

        public void StopRound(int? winnerIndex = null) {
            _MatchEnd = DateTime.UtcNow;

            if (winnerIndex != null) {
                if (_Players.TryGetValue(winnerIndex.Value, out TrackedPlayer? runner) == true) {
                    runner.Wins += 1;
                    _MatchEvents.EmitPlayerUpdateEvent(runner.Index, runner);
                } else {
                    _Logger.LogWarning($"Cannot set winner to index {winnerIndex.Value}, _Players does not have");
                }
            }

            _Logger.LogInformation($"Match finished at {_MatchEnd}");
            _AdminMessages.Log($"Match stopped at {DateTime.UtcNow}");

            SetState(MatchState.FINISHED);
        }

        private void SetState(MatchState state) {
            if (_State == state) {
                _Logger.LogDebug($"Not setting match state to {state}, is the current one");
                return;
            }

            _State = state;
            _MatchEvents.EmitMatchStateEvent(_State);
        }

        private async void KillHandler(object? sender, Ps2EventArgs<KillEvent> args) {
            if (_State != MatchState.RUNNING) {
                return;
            }

            KillEvent ev = args.Payload;

            string sourceFactionID = Loadout.GetFaction(ev.LoadoutID);
            string targetFactionID = Loadout.GetFaction(ev.TargetLoadoutID);

            foreach (KeyValuePair<int, TrackedPlayer> entry in _Players) {
                int index = entry.Key;
                TrackedPlayer player = entry.Value;

                bool emit = false;

                foreach (Character c in player.Characters) {
                    if (ev.SourceID == c.ID && ev.TargetID == c.ID) {
                        _Logger.LogInformation($"Player {index} committed suicide");
                        _MatchMessages.Log($"Team {index}:{player.RunnerName} @{c.Name} SUICIDE");

                        if (player.Streak > 1) {
                            player.Streaks.Add(player.Streak);
                            player.Streak = 0;
                        }

                        player.Deaths.Add(ev);

                        emit = true;
                    } else if (c.ID == ev.SourceID) {
                        if (sourceFactionID == targetFactionID) {
                            _Logger.LogInformation($"Player {index}:{player.RunnerName} on {c.Name} TK");
                            _MatchMessages.Log($"Team {index}:{player.RunnerName} @{c.Name} got a TK");
                        } else {
                            //_Logger.LogInformation($"Player {index}:{player.RunnerName} kill");
                            player.Kills.Add(ev);

                            PsItem? weapon = await _ItemCollection.GetByID(ev.WeaponID);
                            if (weapon != null) {
                                if (ItemCategory.IsValidSpeedrunnerWeapon(weapon) == true) {
                                    player.Streak += 1;
                                    player.ValidKills.Add(ev);

                                    int score = 1;

                                    List<IndexedChallenge> runningChallenges = _Challenges.GetRunning();
                                    foreach (IndexedChallenge challenge in runningChallenges) {
                                        bool met = await challenge.Challenge.WasMet(ev, weapon);

                                        if (_Challenges.GetMode() == ChallengeMode.MEAN) {
                                            if (met == false) {
                                                _Logger.LogTrace($"Team {index}:{player.RunnerName} @{c.Name} failed challenge {challenge.Challenge.ID}/{challenge.Challenge.Name}");
                                                score = 0;
                                            } else {
                                                challenge.KillCount += 1;
                                                _ChallengeEvents.EmitChallengeUpdate(challenge);
                                            }
                                        } else if (_Challenges.GetMode() == ChallengeMode.NICE) {
                                            if (met == true) {
                                                challenge.KillCount += 1;
                                                _ChallengeEvents.EmitChallengeUpdate(challenge);
                                                _Logger.LogTrace($"Team {index}:{player.RunnerName} @{c.Name} met challenge {challenge.Challenge.ID}/{challenge.Challenge.Name}, score mult {challenge.Challenge.Multiplier}");
                                                score *= challenge.Challenge.Multiplier;
                                            }
                                        } else {
                                            _Logger.LogError($"Unknown challenge mode {_Challenges.GetMode()}");
                                        }

                                        if (challenge.Challenge.DurationType == Code.Challenge.ChallengeDurationType.KILLS && challenge.KillCount >= challenge.Challenge.Duration) {
                                            _Logger.LogDebug($"Team {index}:{player.RunnerName} @{c.Name} finished challenge {challenge.Challenge.ID}/{challenge.Challenge.Name}");
                                            _Challenges.End(challenge.Index);
                                        }
                                    }

                                    if (score != 0) {
                                        player.Scores.Add(new ScoreEvent() {
                                            Timestamp = ev.Timestamp,
                                            ScoreChange = score,
                                            TotalScore = player.Score + score
                                        });
                                    }

                                    player.Score += score;

                                    _Logger.LogInformation($"Player {index}:{player.RunnerName} on {c.Name} valid weapon {score} points, {weapon.Name}/{weapon.CategoryID}");
                                    _MatchMessages.Log($"Team {index}:{player.RunnerName} @{c.Name} VALID kill {score} points, {weapon.Name}/{weapon.CategoryID}, faction {targetFactionID}");

                                    if (player.Score >= _Settings.KillGoal) {
                                        _Logger.LogInformation($"Player {index}:{player.RunnerName} reached goal {_Settings.KillGoal}, ending match");
                                        _MatchMessages.Log($"Team {index}:{player.RunnerName} reached goal {_Settings.KillGoal}, ending match");
                                        StopRound(player.Index);
                                    }
                                } else {
                                    _Logger.LogInformation($"Player {index}:{player.RunnerName} on {c.Name} invalid weapon, {weapon.Name}/{weapon.CategoryID}");
                                    _MatchMessages.Log($"Team {index}:{player.RunnerName} @{c.Name} INVALID kill, {weapon.Name}/{weapon.CategoryID}, faction {targetFactionID}");
                                }
                            } else {
                                _MatchMessages.Log($"Team {index}:{player.RunnerName} @{c.Name} UNKNOWN WEAPON {ev.WeaponID}, faction {targetFactionID}");
                                _Logger.LogInformation($"Null weapon {ev.WeaponID}");
                            }

                            emit = true;
                        }
                    } else if (c.ID == ev.TargetID) {
                        if (player.Streak > 1) {
                            player.Streaks.Add(player.Streak);
                        }
                        player.Streak = 0;

                        player.Deaths.Add(ev);

                        _Logger.LogInformation($"Player {index}:{player.RunnerName} on {c.Name} death");
                        _MatchMessages.Log($"Team {index}:{player.RunnerName} @{c.Name} DEATH, faction {sourceFactionID}");

                        emit = true;
                    } else {
                        //_Logger.LogInformation($"Kill source:{ev.SourceID}, target:{ev.TargetID} was not {player.ID}");
                    }
                }

                if (emit == true) {
                    _MatchEvents.EmitPlayerUpdateEvent(index, player);
                }
            }
        }

        private void ExpHandler(object? sender, Ps2EventArgs<ExpEvent> args) {
            if (_State != MatchState.RUNNING) {
                return;
            }

            ExpEvent ev = args.Payload;

            TrackedPlayer? runner = _GetRunnerFromID(ev.SourceID);
            if (runner == null) {
                runner = _GetRunnerFromID(ev.TargetID);
            }

            if (runner == null) {
                return;
            }

            runner.Exp.Add(ev);

            if (_IsAssistEvent(ev.ExpID)) {
                Character c = _GetCharacterFromID(ev.SourceID)
                    ?? _GetCharacterFromID(ev.TargetID)
                    ?? throw new ArgumentNullException($"Expected character ID {ev.SourceID} or {ev.TargetID} to exist, on team {runner.Index}:{runner.RunnerName}");

                _MatchMessages.Log($"Team {runner.Index}:{runner.RunnerName} @{c.Name} ASSIST");
            }
        }

        private bool _IsAssistEvent(int expID) {
            return expID == Experience.ASSIST || expID == Experience.PRIORITY_ASSIST
                || expID == Experience.SPAWN_ASSIST || expID == Experience.HIGH_PRIORITY_ASSIST;
        }

        private TrackedPlayer? _GetRunnerFromID(string charID) {
            foreach (KeyValuePair<int, TrackedPlayer> entry in _Players) {
                foreach (Character c in entry.Value.Characters) {
                    if (c.ID == charID) {
                        return entry.Value;
                    }
                }
            }

            return null;
        }

        private Character? _GetCharacterFromID(string charID) {
            foreach (KeyValuePair<int, TrackedPlayer> entry in _Players) {
                foreach (Character c in entry.Value.Characters) {
                    if (c.ID == charID) {
                        return c;
                    }
                }
            }

            return null;
        }

        public MatchState GetState() => _State;
        public DateTime GetMatchStart() => _MatchStart;
        public DateTime? GetMatchEnd() => _MatchEnd;
        public List<TrackedPlayer> GetPlayers() => _Players.Values.ToList();
        public int GetMatchLength() => (int)Math.Round(_MatchTicks / TICKS_PER_SECOND);
        public MatchSettings GetSettings() => _Settings;

    }
}
