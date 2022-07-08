using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using watchtower.Census;
using watchtower.Code;
using watchtower.Code.Census.Implementations;
using watchtower.Code.Challenge;
using watchtower.Code.Constants;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Census;
using watchtower.Models.Events;
using watchtower.Realtime;
using watchtower.Services.Queue;

namespace watchtower.Services {

    /// <summary>
    /// Singleton service to manage a match
    /// </summary>
    public class MatchManager {

        /// <summary>
        /// How many times the timer will tick per second
        /// </summary>
        const double TICKS_PER_SECOND = 10000000D;

        private readonly ILogger<MatchManager> _Logger;

        private readonly ICharacterCollection _CharacterColleciton;
        private readonly IItemCollection _ItemCollection;
        private readonly ExperienceCollection _ExpCollection;

        private readonly IRealtimeMonitor _Realtime;
        private readonly IRealtimeEventBroadcastService _RealtimeEvents;
        private readonly IMatchEventBroadcastService _MatchEvents;
        private readonly IMatchMessageBroadcastService _MatchMessages;
        private readonly IAdminMessageBroadcastService _AdminMessages;
        private readonly IChallengeManager _Challenges;
        private readonly IChallengeEventBroadcastService _ChallengeEvents;
        private readonly ISecondTimer _Timer;

        private readonly DiscordThreadManager _ThreadManager;

        private RoundState _RoundState = RoundState.UNSTARTED;
        private MatchState _MatchState = MatchState.UNSTARTED;
        private DateTime _MatchStart = DateTime.UtcNow;
        private DateTime? _MatchEnd = null;
        private long _MatchTicks = 0;

        private DateTime? _LastAutoChallenge = null;
        private bool _PendingAutoChallenge = false;

        private readonly Dictionary<int, TrackedPlayer> _Players = new Dictionary<int, TrackedPlayer>();
        private MatchSettings _Settings = new MatchSettings();
        private AutoChallengeSettings _AutoSettings = new AutoChallengeSettings();

        public MatchManager(ILogger<MatchManager> logger,
                ICharacterCollection charColl, IItemCollection itemColl,
                IRealtimeEventBroadcastService events, IMatchEventBroadcastService matchEvents,
                IRealtimeMonitor realtime, IChallengeEventBroadcastService challengeEvents,
                IMatchMessageBroadcastService matchMessages, IAdminMessageBroadcastService adminMessages,
                IChallengeManager challenges, ISecondTimer timer,
                ExperienceCollection expColl, DiscordThreadManager threadManager) {

            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _CharacterColleciton = charColl ?? throw new ArgumentNullException(nameof(charColl));
            _ItemCollection = itemColl ?? throw new ArgumentNullException(nameof(itemColl));
            _ExpCollection = expColl ?? throw new ArgumentNullException(nameof(expColl));

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
            _ThreadManager = threadManager;
        }

        /// <summary>
        /// Add the realtime listeners used to give score
        /// </summary>
        private void AddListeners() {
            _RealtimeEvents.OnKillEvent += KillHandler;
            _RealtimeEvents.OnExpEvent += ExpHandler;

            _Timer.OnTick += OnTick;
        }

        /// <summary>
        /// Add a new character to a runner. If no runner at the index has been set, a new runner is created
        /// </summary>
        /// <param name="index">Index of the runner to add the character to</param>
        /// <param name="charName">Name of the character to add. Case insensitive</param>
        /// <returns>If the character was successfully added or not</returns>
        public async Task<bool> AddCharacter(int index, string charName) {
            if (_MatchState != MatchState.STARTED) {
                return false;
            }

            if (_Players.TryGetValue(index, out TrackedPlayer? player) == false) {
                player = new TrackedPlayer {
                    Index = index,
                    RunnerName = $"Runner {index + 1}"
                };

                _Players.Add(index, player);
                bool res = await _ThreadManager.CreateScoreMessage(index, player.RunnerName);
                if (res == false) {
                    _AdminMessages.Log($"Failed to create score embed in match thread");
                }

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
                await _ThreadManager.UpdateRunnerScore(index, player.RunnerName, player.Score);
            }

            player.Characters.Add(ch);

            _Realtime.Subscribe(new Subscription() {
                Characters = { ch.ID },
                Events = { 
                    "Death",
                    "GainExperience" 
                }
            });

            string s = $"Team {index}:{player.RunnerName} added character {charName}";
            _AdminMessages.Log(s);

            _MatchEvents.EmitPlayerUpdateEvent(index, player);

            return true;
        }

        /// <summary>
        /// Remove a character from a team
        /// </summary>
        /// <param name="index">Index of the team to remove the character from</param>
        /// <param name="charName">Name of the character to be removed</param>
        public void RemoveCharacter(int index, string charName) {
            if (_Players.TryGetValue(index, out TrackedPlayer? player) == true) {
                player.Characters = player.Characters.Where(iter => iter.Name.ToLower() != charName.ToLower()).ToList();
                _AdminMessages.Log($"Team {index}:{player.RunnerName} removed character {charName}");
            } else {
                _Logger.LogWarning($"Cannot remove {charName} from player {index} cause it wasn't found");
            }
        }

        /// <summary>
        /// Set the settings used in a match
        /// </summary>
        /// <param name="settings">Settings to use in the match</param>
        public void SetSettings(MatchSettings settings) {
            if (_RoundState == RoundState.RUNNING) {
                _Logger.LogWarning($"Match is currently running, some settings may create funky behavior");
            }

            _Settings = settings;

            _Logger.LogInformation($"Match settings:" +
                $"\n\tKillGoal: {_Settings.KillGoal}" + $"\n\tTimeGoal:{_Settings.TimeGoal}"
            );

            _MatchEvents.EmitMatchSettingsEvent(_Settings);
        }

        /// <summary>
        /// Set the settings used for auto challenge
        /// </summary>
        /// <param name="auto">Settings</param>
        public void SetAutoChallengeSettings(AutoChallengeSettings auto) {
            if (_RoundState == RoundState.RUNNING) {
                _Logger.LogWarning($"Not changing auto challenge settings, as match is running");
                return;
            }

            _AutoSettings = auto;
            _MatchEvents.EmitAutoSettingsChange(_AutoSettings);
        }

        /// <summary>
        /// Set the name of a runner
        /// </summary>
        /// <param name="index">Index of the runner to set</param>
        /// <param name="playerName">Name of the runner to set</param>
        public void SetRunnerName(int index, string? runnerName) {
            if (_Players.TryGetValue(index, out TrackedPlayer? player) == true) {
                string name = runnerName ?? $"Runner {index + 1}";

                player.RunnerName = name;

                _ = _ThreadManager.UpdateRunnerScore(index, name, player.Score);
            } else {
                _Logger.LogWarning($"Cannot set runner name for {index}, not in _Players");
            }
        }

        /// <summary>
        /// Increment the score of a team based on the index
        /// </summary>
        /// <param name="index">Index of the team to increment the score of</param>
        public void IncrementScore(int index) {
            if (_Players.TryGetValue(index, out TrackedPlayer? player) == true) {
                player.Streak++;
                SetScore(index, player.Score + 1);
            }
        }

        /// <summary>
        /// Set the score of a runner
        /// </summary>
        /// <param name="index">Index of the runner to set the score of</param>
        /// <param name="score">Score to set the runner to</param>
        public void SetScore(int index, int score) {
            if (_Players.TryGetValue(index, out TrackedPlayer? player) == true) {
                player.Score = score;
                _MatchEvents.EmitPlayerUpdateEvent(player.Index, player);

                _ = _ThreadManager.UpdateRunnerScore(index, player.RunnerName, score);

                if (player.Score >= _Settings.KillGoal) {
                    _MatchMessages.Log($"Team {index}:{player.RunnerName} reached goal {_Settings.KillGoal}, ending match");
                    _ = StopRound(index);
                }
            } else {
                _Logger.LogWarning($"Cannot set score of runner {index}, _Players does not contain");
            }

            if (_PendingAutoChallenge == false) {
                if (_LastAutoChallenge != null) {
                    TimeSpan diff = DateTime.UtcNow - _LastAutoChallenge.Value;
                    if (diff < TimeSpan.FromMinutes(5)) {
                        _PendingAutoChallenge = true;
                        _Logger.LogInformation($"Last auto challenge was started {diff} ago, setting pending and waiting");
                        _MatchMessages.Log($"Last auto challenge was started {diff} ago, setting pending and waiting");
                        return;
                    }
                }

                int totalKillCount = 0;
                foreach (KeyValuePair<int, TrackedPlayer> iter in _Players) {
                    totalKillCount += iter.Value.ValidKills.Count;
                }

                if (totalKillCount % _AutoSettings.KillSpanInterval == 0) {
                    _Logger.LogInformation($"Kill count interval hit, starting auto challenge");
                    _MatchMessages.Log($"Hit a total of {totalKillCount} kills, starting auto challenge");

                    StartAutoChallenge();
                }
            }
        }

        /// <summary>
        /// Get the score of a runner, or null if it doesn't exist
        /// </summary>
        /// <param name="index">Index of the runner to get the score of</param>
        public int? GetScore(int index) {
            if (_Players.TryGetValue(index, out TrackedPlayer? player) == true) {
                return player.Score;
            }

            _Logger.LogWarning($"Cannot get score of runner {index}, _Players does not contain");
            return null;
        }

        /// <summary>
        /// Get the runner being tracked
        /// </summary>
        /// <param name="index">Index of the runner to get</param>
        public TrackedPlayer? GetPlayer(int index) {
            if (_Players.TryGetValue(index, out TrackedPlayer? player) == true) {
                return player;
            }
            return null;
        }

        /// <summary>
        /// Occurs every time the match time keeping tick occurs
        /// </summary>
        private void OnTick(object? sender, SecondTimerArgs args) {
            if (_RoundState != RoundState.RUNNING) {
                return;
            }

            if (_Settings.TimeGoal != 0){
                if (GetMatchLength() >= _Settings.TimeGoal - 1){
                    _MatchMessages.Log($"Time goal reached, ending match");
                    StopRound(null);
                }
            }

            _MatchTicks += args.ElapsedTicks;

            int matchLength = (int)Math.Round(_MatchTicks / TICKS_PER_SECOND);

            _MatchEvents.EmitTimerEvent(matchLength);

            if (_PendingAutoChallenge == true) {
                if (_LastAutoChallenge == null) {
                    StartAutoChallenge();
                } else {
                    TimeSpan diff = DateTime.UtcNow - _LastAutoChallenge.Value;
                    if (diff > TimeSpan.FromMinutes(5)) {
                        string s = $"starting pending auto challenge, it's been {diff}";
                        _Logger.LogInformation(s);
                        _AdminMessages.Log(s);

                        StartAutoChallenge();
                    }
                }
            }

            /*
            if (_AutoSettings.Enabled) {
                if ((matchLength - _AutoSettings.StartDelay) % _AutoSettings.Interval == 0) {
                    _Logger.LogInformation($"Starting new auto challenge");
                    StartAutoChallenge();
                }
            }
            */

            foreach (IndexedChallenge entry in _Challenges.GetRunning()) {
                if (entry.Challenge.DurationType != ChallengeDurationType.TIMED) {
                    continue;
                }

                entry.TickCount += args.ElapsedTicks;
                //_Logger.LogTrace($"{entry.Index} {entry.Challenge.ID}/{entry.Challenge.Name} total ticks: {entry.TickCount}");

                _ChallengeEvents.EmitChallengeUpdate(entry);

                if ((int)Math.Round(entry.TickCount / TICKS_PER_SECOND) > entry.Challenge.Duration) {
                    _Logger.LogDebug($"{entry.Index} {entry.Challenge.ID}/{entry.Challenge.Name} done");
                    _Challenges.End(entry.Index);
                }
            }
        }

        /// <summary>
        /// Start a match, which will create the thread that speedrunners can join to view the active challenges
        /// </summary>
        public async Task StartMatch() {
            if (_MatchState != MatchState.UNSTARTED) {
                _Logger.LogWarning($"Cannot start match, state is not UNSTARTED, currently is {_MatchState}");
                return;
            }

            SetMatchState(MatchState.STARTED);

            if (await _ThreadManager.CreateMatchThread() == false) {
                _Logger.LogWarning($"Failed to create match thread");
                _AdminMessages.Log($"Failed to create match thread");
                return;
            }

            if (await _ThreadManager.ConnectToVoice() == false) {
                _Logger.LogWarning($"Failed to connect to voice");
            }
            _Logger.LogInformation($"Successfully started match");
        }

        /// <summary>
        /// End a match, cleaning up the threads made and leave the voice channel
        /// </summary>
        public async Task EndMatch() {
            if (_MatchState != MatchState.STARTED) {
                _Logger.LogWarning($"Cannot end match, state is not STARTED, currently is {_MatchState}");
                return;
            }

            if (await _ThreadManager.CloseThread() == false) {
                _Logger.LogWarning($"Failed to close message thread");
            }

            if (await _ThreadManager.DisconnectFromVoice() == false) {
                _Logger.LogWarning($"Failed to disconnect from voice");
            }

            SetMatchState(MatchState.UNSTARTED);
        }

        /// <summary>
        /// Start a round. If the match is already running, nothing happens
        /// </summary>
        public async Task StartRound() {
            if (_RoundState == RoundState.RUNNING) {
                _Logger.LogWarning($"Not starting match, already started");
                return;
            }

            if (_RoundState == RoundState.UNSTARTED) {
                _MatchTicks = 0;
                _MatchStart = DateTime.UtcNow;
                _AdminMessages.Log($"Match unstarted, resetting ticks and start");
            }

            SetRoundState(RoundState.RUNNING);

            _AdminMessages.Log($"Match started at {_MatchStart}");
            await _ThreadManager.SendThreadMessage($"Round started at {_MatchStart.GetDiscordFormat()}");
        }

        /// <summary>
        /// Clear the match, resetting everything for another match
        /// </summary>
        public Task ClearMatch() {
            foreach (TrackedPlayer player in GetPlayers()) {
                player.Score = 0;
                player.Scores = new List<ScoreEvent>();
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

            SetRoundState(RoundState.UNSTARTED);
            _MatchEvents.EmitTimerEvent(0);

            _MatchStart = DateTime.UtcNow;
            _MatchEnd = null;

            _AdminMessages.Clear();
            _MatchMessages.Clear();

            _AdminMessages.Log($"Match cleared at {DateTime.UtcNow}");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Restart an existing round. If a match is not running, nothing happens
        /// </summary>
        public Task RestartRound() {
            _MatchStart = DateTime.UtcNow;
            _MatchEnd = null;
            _MatchTicks = 0;

            _MatchEvents.EmitTimerEvent(0);

            foreach (TrackedPlayer player in GetPlayers()) {
                player.Score = 0;
                player.Scores = new List<ScoreEvent>();
                player.Kills = new List<KillEvent>();
                player.ValidKills = new List<KillEvent>();
                player.Deaths = new List<KillEvent>();
                player.Exp = new List<ExpEvent>();
                player.Streak = 0;
                player.Streaks = new List<int>();
            }

            SetRoundState(RoundState.UNSTARTED);

            _AdminMessages.Log($"Match restarted at {DateTime.UtcNow}");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Pause a currently running round
        /// </summary>
        public async Task PauseRound() {
            SetRoundState(RoundState.PAUSED);

            _AdminMessages.Log($"Round paused at {DateTime.UtcNow:u}");

            await _ThreadManager.SendThreadMessage($"Round paused at {DateTime.UtcNow.GetDiscordFormat()}");
        }

        /// <summary>
        /// Stop the current round. Does nothing if a round isn't running
        /// </summary>
        public async Task StopRound(int? winnerIndex = null) {
            _MatchEnd = DateTime.UtcNow;

            string s = $"Round over at {DateTime.UtcNow.GetDiscordFormat()}";

            if (winnerIndex != null) {
                if (_Players.TryGetValue(winnerIndex.Value, out TrackedPlayer? runner) == true) {
                    runner.Wins += 1;
                    _MatchEvents.EmitPlayerUpdateEvent(runner.Index, runner);

                    s += $"\n**Winner:** {runner.RunnerName}";
                } else {
                    s += $"ERROR: Cannot set winner to index {winnerIndex.Value}, _Players does not have";
                    _Logger.LogWarning($"Cannot set winner to index {winnerIndex.Value}, _Players does not have");
                }
            }

            _Logger.LogInformation($"Match finished at {_MatchEnd:u}");
            _AdminMessages.Log($"Match stopped at {_MatchEnd:u}");
            await _ThreadManager.SendThreadMessage(s);

            SetRoundState(RoundState.FINISHED);
        }

        /// <summary>
        /// Start a new auto challenge
        /// </summary>
        private void StartAutoChallenge() {
            _PendingAutoChallenge = false;
            _LastAutoChallenge = DateTime.UtcNow;

            if (_AutoSettings.OptionCount <= 0) {
                _Logger.LogWarning($"Cannot start auto poll, there are 0 options");
                return;
            }

            List<IRunChallenge> challenges = _Challenges.GetActive().Shuffle();
            if (_AutoSettings.OptionCount > challenges.Count) {
                _Logger.LogWarning($"Setting auto challenge option count to {challenges.Count}, was {_AutoSettings.OptionCount}, which is more than options available");
                _AutoSettings.OptionCount = challenges.Count;
            }

            ChallengePollOptions options = new ChallengePollOptions() {
                Possible = challenges.Take(_AutoSettings.OptionCount).Select(i => i.ID).ToList(),
                VoteTime = _AutoSettings.PollTime
            };

            _Challenges.StartPoll(options);
        }

        /// <summary>
        /// Helper to set the round state, and also emit the round state change
        /// </summary>
        private void SetRoundState(RoundState state) {
            if (_RoundState == state) {
                _Logger.LogDebug($"Not setting round state to {state}, is the current one");
                return;
            }

            _RoundState = state;
            _MatchEvents.EmitRoundStateEvent(_RoundState);
        }

        /// <summary>
        /// Helper to set the match state, and also emit the match state change
        /// </summary>
        private void SetMatchState(MatchState state) {
            if (_MatchState == state) {
                _Logger.LogDebug($"Not setting match state to {state}, is the current one");
                return;
            }

            _MatchState = state;
            _MatchEvents.EmitMatchStateEvent(_MatchState);
        }

        /// <summary>
        /// Handler whenever a Death event comes from the Realtime Census sockets
        /// </summary>
        private async void KillHandler(object? sender, Ps2EventArgs<KillEvent> args) {
            if (_RoundState != RoundState.RUNNING) {
                return;
            }

            KillEvent ev = args.Payload;

            string sourceFactionID = Loadout.GetFaction(ev.LoadoutID);
            string targetFactionID = Loadout.GetFaction(ev.TargetLoadoutID);

            bool emit = false;

            foreach (KeyValuePair<int, TrackedPlayer> entry in _Players) {
                int index = entry.Key;
                TrackedPlayer player = entry.Value;

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
                        emit = await HandleNotSuicideKill(args, index, player, c);
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

        /// <summary>
        /// Split the code up a bit it was getting BIG
        /// </summary>
        /// <param name="args"></param>
        /// <param name="index"></param>
        /// <param name="player"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private async Task<bool> HandleNotSuicideKill(Ps2EventArgs<KillEvent> args, int index, TrackedPlayer player, Character c) {
            KillEvent ev = args.Payload;

            string sourceFactionID = Loadout.GetFaction(ev.LoadoutID);
            string targetFactionID = Loadout.GetFaction(ev.TargetLoadoutID);

            bool emit = false;

            if (sourceFactionID == targetFactionID) {
                _Logger.LogInformation($"Player {index}:{player.RunnerName} on {c.Name} TK");
                _MatchMessages.Log($"Team {index}:{player.RunnerName} @{c.Name} got a TK");
            } else {
                //_Logger.LogInformation($"Player {index}:{player.RunnerName} kill");
                player.Kills.Add(ev);

                if (targetFactionID == "4") {
                    // Wait for the EXP events to show up
                    await Task.Delay(1000);

                    ExpEvent? expEvent = null;
                    for (int i = player.Exp.Count - 1; i >= 0; --i) {
                        ExpEvent exp = player.Exp[i];
                        //_Logger.LogTrace($"Finding exp event from {i}, got {exp.ExpID} {exp.Timestamp}, looking for timestamp {ev.Timestamp}");
                        if (exp.Timestamp < ev.Timestamp) {
                            _Logger.LogTrace($"{exp.Timestamp} is less than {ev.Timestamp}, leaving now");
                            break;
                        }

                        if (exp.Timestamp == ev.Timestamp && Experience.IsKill(exp.ExpID) && exp.TargetID == ev.TargetID) {
                            //_Logger.LogTrace($"Found {ev.Timestamp} in {exp.ExpID} {exp.Timestamp}");
                            expEvent = exp;
                            break;
                        }
                    }

                    if (expEvent == null) {
                        _MatchMessages.Log($"Team {index}:{player.RunnerName} @{c.Name} Missing kill exp event, assuming to be a TK");
                        return false;
                    }
                }

                _MatchMessages.Log($"Team {index}:{player.RunnerName} @{c.Name} exp event for kill");

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

                            if (challenge.Challenge.DurationType == ChallengeDurationType.KILLS && challenge.KillCount >= challenge.Challenge.Duration) {
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

                        if (player.Score >= _Settings.KillGoal && _Settings.KillGoal != 0) {
                            _Logger.LogInformation($"Player {index}:{player.RunnerName} reached goal {_Settings.KillGoal}, ending match");
                            _MatchMessages.Log($"Team {index}:{player.RunnerName} reached goal {_Settings.KillGoal}, ending match");
                            await StopRound(player.Index);
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

            return emit;
        }

        /// <summary>
        /// Experience handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void ExpHandler(object? sender, Ps2EventArgs<ExpEvent> args) {
            if (_RoundState != RoundState.RUNNING) {
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

            string direction = "SOURCE";
            Character? c = _GetCharacterFromID(ev.SourceID);

            if (c == null) {
                direction = "TARGET";
                c = _GetCharacterFromID(ev.TargetID);
            }

            if (c == null) {
                direction = "UNKNOWN";
            }

            ExpEntry? entry = await _ExpCollection.GetByID(ev.ExpID);
            _MatchMessages.Log($"Team {runner.Index}:{runner.RunnerName} @{c?.Name} {direction} {entry?.Description ?? $"missing {ev.ExpID}"}");
        }

        /// <summary>
        /// Get the team based on the character ID
        /// </summary>
        /// <param name="charID"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get the character based on the character ID
        /// </summary>
        /// <param name="charID"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get the current state of the round
        /// </summary>
        public RoundState GetRoundState() => _RoundState;

        /// <summary>
        /// Get the current state of the match
        /// </summary>
        public MatchState GetMatchState() => _MatchState;

        /// <summary>
        /// Get the <c>DateTime</c> of when a match was started
        /// </summary>
        public DateTime GetMatchStart() => _MatchStart;

        /// <summary>
        /// Get the <c>DateTime</c> of when the match ended, or <c>null</c> if it hasn't ended
        /// </summary>
        public DateTime? GetMatchEnd() => _MatchEnd;

        /// <summary>
        /// Get all runners in this match
        /// </summary>
        /// <returns>The list of runners</returns>
        public List<TrackedPlayer> GetPlayers() => _Players.Values.ToList();

        /// <summary>
        /// Get how many seconds a match has been running for. Not really useful if the match has not started
        /// </summary>
        public int GetMatchLength() => (int)Math.Round(_MatchTicks / TICKS_PER_SECOND);

        /// <summary>
        /// Get the current settings in a match
        /// </summary>
        public MatchSettings GetSettings() => _Settings;

        /// <summary>
        /// Get the auto challenge settings
        /// </summary>
        public AutoChallengeSettings GetAutoChallengeSettings() => _AutoSettings;

    }

}
