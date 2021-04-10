using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using watchtower.Census;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Census;
using watchtower.Models.Events;
using watchtower.Realtime;

namespace watchtower.Services {

    public class MatchManager : IMatchManager {

        private readonly ILogger<MatchManager> _Logger;

        private readonly ICharacterCollection _CharacterColleciton;
        private readonly IItemCollection _ItemCollection;
        private readonly IEventBroadcastService _Events;
        private readonly IRealtimeMonitor _Realtime;

        private readonly Dictionary<int, TrackedPlayer> _Players = new Dictionary<int, TrackedPlayer>();

        private MatchState _State = MatchState.UNSTARTED;

        private readonly Timer _MatchTimer;
        private DateTime _LastTimerTick = DateTime.UtcNow;

        private DateTime _MatchStart = DateTime.UtcNow;
        private DateTime? _MatchEnd = null;

        private long _MatchTicks = 0;
        const double TICKS_PER_SECOND = 10000000D;

        private MatchSettings _Settings = new MatchSettings();

        public MatchManager(ILogger<MatchManager> logger,
                ICharacterCollection charColl, IItemCollection itemColl,
                IEventBroadcastService events, IRealtimeMonitor realtime) {

            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _CharacterColleciton = charColl ?? throw new ArgumentNullException(nameof(charColl));
            _ItemCollection = itemColl ?? throw new ArgumentNullException(nameof(itemColl));
            _Events = events ?? throw new ArgumentNullException(nameof(events));
            _Realtime = realtime ?? throw new ArgumentNullException(nameof(realtime));

            _MatchTimer = new Timer(1000D);

            SetSettings(new MatchSettings());

            AddListeners();
        }

        private void AddListeners() {
            _Events.OnKillEvent += KillHandler;
            _MatchTimer.Elapsed += OnTimerTick;
        }

        public async Task<bool> AddCharacter(int index, string charName) {
            if (_Players.TryGetValue(index, out TrackedPlayer? player) == false) {
                player = new TrackedPlayer {
                    Index = index,
                    RunnerName = $"Runner {index + 1}"
                };

                _Players.Add(index, player);
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
                player.RunnerName = ch.Name;
            }

            player.Characters.Add(ch);

            _Realtime.Subscribe(new Subscription() {
                Characters = { ch.ID },
                Events = { "Death" }
            });

            _Events.EmitPlayerUpdateEvent(index, player);

            return true;
        }

        public void RemoveCharacter(int index, string charName) {
            if (_Players.TryGetValue(index, out TrackedPlayer? player) == true) {
                player.Characters = player.Characters.Where(iter => iter.Name.ToLower() != charName.ToLower()).ToList();
            } else {
                _Logger.LogWarning($"Cannot remove {charName} from player {index} cause it wasn't found");
            }
        }

        public MatchSettings GetSettings() => _Settings;

        public void SetSettings(MatchSettings settings) {
            if (_State == MatchState.RUNNING) {
                _Logger.LogWarning($"Match is currently running, some settings may create funky behavior");
            }

            _Settings = settings;

            _Logger.LogInformation($"Match settings:" +
                $"\n\tKillGoal: {_Settings.KillGoal}"
            );

            _Events.EmitMatchSettingsEvent(_Settings);
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
                _Events.EmitPlayerUpdateEvent(player.Index, player);
            } else {
                _Logger.LogWarning($"Cannot set score of runner {index}, _Players does not contain");
            }
        }

        public int GetScore(int index) {
            if (_Players.TryGetValue(index, out TrackedPlayer? player) == true) {
                return player.Score;
            } else {
                _Logger.LogWarning($"Cannot get score of runner {index}, _Players does not contain");
                return -1;
            }
        }

        public TrackedPlayer? GetPlayer(int index) {
            if (_Players.TryGetValue(index, out TrackedPlayer? player) == true) {
                return player;
            }
            return null;
        }

        private void OnTimerTick(object? sender, ElapsedEventArgs args) {
            DateTime time = args.SignalTime.ToUniversalTime();

            long nowTicks = time.Ticks;
            long prevTicks = _LastTimerTick.Ticks;

            _MatchTicks += nowTicks - prevTicks;

            _Logger.LogDebug($"Total ticks: {_MatchTicks}, seconds {Math.Round(_MatchTicks / TICKS_PER_SECOND)}");

            _Events.EmitTimerEvent((int)Math.Round(_MatchTicks / TICKS_PER_SECOND));

            _LastTimerTick = DateTime.UtcNow;
        }

        public void StartRound() {
            if (_State == MatchState.RUNNING) {
                _Logger.LogWarning($"Not starting match, already started");
                return;
            }

            _MatchTimer.AutoReset = true;
            _MatchTimer.Start();
            _LastTimerTick = DateTime.UtcNow;

            SetState(MatchState.RUNNING);
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
            }

            _Players.Clear();

            _MatchTimer.Stop();
            _MatchTicks = 0;

            SetState(MatchState.UNSTARTED);
            _Events.EmitTimerEvent(0);

            _MatchStart = DateTime.UtcNow;
            _MatchEnd = null;
        }

        public void RestartRound() {
            _MatchStart = DateTime.UtcNow;
            _MatchEnd = null;
            _MatchTicks = 0;

            _Events.EmitTimerEvent(0);

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
        }

        public void ResetRound() {
            _MatchStart = DateTime.UtcNow;
            _MatchEnd = null;
            _MatchTicks = 0;

            _Events.EmitTimerEvent(0);
            _MatchTimer.Stop();

            foreach (TrackedPlayer player in GetPlayers()) {
                player.Score = 0;
                player.Kills = new List<KillEvent>();
                player.ValidKills = new List<KillEvent>();
                player.Deaths = new List<KillEvent>();
                player.Exp = new List<ExpEvent>();
                player.Streak = 0;
                player.Streaks = new List<int>();
                player.Characters = new List<Character>();
            }

            SetState(MatchState.UNSTARTED);
        }

        public void PauseRound() {
            _MatchTimer.Stop();

            SetState(MatchState.PAUSED);
        }

        public void StopRound() {
            _MatchTimer.Stop();
            _MatchEnd = DateTime.UtcNow;
            _MatchTicks = 0;

            _Logger.LogInformation($"Match finished at {_MatchEnd}");

            SetState(MatchState.FINISHED);
        }

        private void SetState(MatchState state) {
            if (_State == state) {
                _Logger.LogDebug($"Not setting match state to {state}, is the current one");
                return;
            }

            _State = state;
            _Events.EmitMatchStateEvent(_State);
        }

        public MatchState GetState() => _State;

        public DateTime GetMatchStart() => _MatchStart;

        public DateTime? GetMatchEnd() => _MatchEnd;

        public List<TrackedPlayer> GetPlayers() => _Players.Values.ToList();

        public int GetMatchLength() {
            return (int)Math.Round(_MatchTicks / TICKS_PER_SECOND);
            /*
            DateTime start = GetMatchStart();
            DateTime end = GetMatchEnd() ?? DateTime.UtcNow;

            TimeSpan startSpan = new TimeSpan(start.Ticks);
            TimeSpan endSpan = new TimeSpan(end.Ticks);

            return (int) (endSpan - startSpan).TotalSeconds;
            */
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

                        if (player.Streak > 1) {
                            player.Streaks.Add(player.Streak);
                            player.Streak = 0;
                        }

                        player.Deaths.Add(ev);

                        emit = true;
                    } else if (c.ID == ev.SourceID) {
                        if (sourceFactionID == targetFactionID) {
                            _Logger.LogInformation($"Player {index}:{player.RunnerName} on {c.Name} TK");
                        } else {
                            //_Logger.LogInformation($"Player {index}:{player.RunnerName} kill");
                            player.Kills.Add(ev);

                            PsItem? weapon = await _ItemCollection.GetByIDAsync(ev.WeaponID);
                            if (weapon != null) {
                                if (ItemCategory.IsValidSpeedrunnerWeapon(weapon) == true) {
                                    player.Streak += 1;
                                    player.Score += 1;
                                    player.ValidKills.Add(ev);
                                    _Logger.LogInformation($"Player {index}:{player.RunnerName} on {c.Name} valid weapon, {weapon.Name}/{weapon.CategoryID}");

                                    if (player.Score >= _Settings.KillGoal) {
                                        _Logger.LogInformation($"Player {index}:{player.RunnerName} reached goal {_Settings.KillGoal}, ending match");
                                        StopRound();
                                    }
                                } else {
                                    _Logger.LogInformation($"Player {index}:{player.RunnerName} on {c.Name} invalid weapon, {weapon.Name}/{weapon.CategoryID}");
                                }
                            } else {
                                _Logger.LogInformation($"Null weapon {ev.WeaponID}");
                            }

                            emit = true;
                        }
                    } else if (c.ID == ev.TargetID) {
                        _Logger.LogInformation($"Player {index}:{player.RunnerName} on {c.Name} death");
                        if (player.Streak > 1) {
                            player.Streaks.Add(player.Streak);
                        }
                        player.Streak = 0;

                        player.Deaths.Add(ev);

                        emit = true;
                    } else {
                        //_Logger.LogInformation($"Kill source:{ev.SourceID}, target:{ev.TargetID} was not {player.ID}");
                    }
                }

                if (emit == true) {
                    _Events.EmitPlayerUpdateEvent(index, player);
                }
            }

        }

    }
}
