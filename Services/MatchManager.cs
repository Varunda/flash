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

        private ILogger<MatchManager> _Logger;

        private ICharacterCollection _CharacterColleciton;
        private IItemCollection _ItemCollection;
        private IEventBroadcastService _Events;
        private IRealtimeMonitor _Realtime;

        private Dictionary<int, TrackedPlayer> _Players = new Dictionary<int, TrackedPlayer>();

        private MatchState _State = MatchState.UNSTARTED;

        private Timer _MatchTimer;
        private DateTime _LastTimerTick = DateTime.UtcNow;

        private DateTime _MatchStart = DateTime.UtcNow;
        private DateTime? _MatchEnd = null;

        public MatchManager(ILogger<MatchManager> logger,
                ICharacterCollection charColl, IItemCollection itemColl,
                IEventBroadcastService events, IRealtimeMonitor realtime) {

            _Logger = logger;

            _CharacterColleciton = charColl ?? throw new ArgumentNullException(nameof(charColl));
            _ItemCollection = itemColl ?? throw new ArgumentNullException(nameof(itemColl));
            _Events = events ?? throw new ArgumentNullException(nameof(events));
            _Realtime = realtime ?? throw new ArgumentNullException(nameof(realtime));

            _MatchTimer = new Timer(1000D);

            AddListeners();
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

                if (ev.SourceID == player.ID && ev.TargetID == player.ID) {
                    _Logger.LogInformation($"Player {index} committed suicide");

                    if (player.Streak > 1) {
                        player.Streaks.Add(player.Streak);
                        player.Streak = 0;
                    }

                    player.Deaths.Add(ev);

                    emit = true;
                } else if (player.ID == ev.SourceID) {
                    if (sourceFactionID == targetFactionID) {
                        _Logger.LogInformation($"Player {index}:{player.RunnerName} TK");
                    } else {
                        //_Logger.LogInformation($"Player {index}:{player.RunnerName} kill");
                        player.Kills.Add(ev);

                        PsItem? weapon = await _ItemCollection.GetByIDAsync(ev.WeaponID);
                        if (weapon != null) {
                            if (ItemCategory.IsValidSpeedrunnerWeapon(weapon) == true) {
                                player.Streak += 1;
                                player.Score += 1;
                                player.ValidKills.Add(ev);
                                _Logger.LogInformation($"Player {index}:{player.RunnerName} valid weapon, {weapon.Name}/{weapon.CategoryID}");

                                if (player.Score == 100) {
                                    _Logger.LogInformation($"Player {index}:{player.RunnerName} reached goal, ending match");
                                    Stop();
                                }
                            } else {
                                _Logger.LogInformation($"Player {index}:{player.RunnerName} invalid weapon, {weapon.Name}/{weapon.CategoryID}");
                            }
                        } else {
                            _Logger.LogInformation($"Null weapon {ev.WeaponID}");
                        }

                        emit = true;
                    }
                } else if (player.ID == ev.TargetID) {
                    _Logger.LogInformation($"Player {index}:{player.RunnerName} death");
                    if (player.Streak > 1) {
                        player.Streaks.Add(player.Streak);
                    }
                    player.Streak = 0;

                    player.Deaths.Add(ev);

                    emit = true;
                } else {
                    //_Logger.LogInformation($"Kill source:{ev.SourceID}, target:{ev.TargetID} was not {player.ID}");
                }

                if (emit == true) {
                    _Events.EmitPlayerUpdateEvent(index, player);
                }
            }

        }

        public async Task SetPlayer(int index, string charName, string? playerName) {
            if (playerName == "") {
                playerName = null;
            }

            if (_Players.ContainsKey(index) == true) {
                TrackedPlayer p = _Players[index];
                if (playerName != null && p.CharacterName.ToLower() == charName.ToLower()) {
                    p.RunnerName = playerName;
                    _Events.EmitPlayerUpdateEvent(index, p);
                    return;
                }
            }

            Character? c = await _CharacterColleciton.GetByNameAsync(charName);
            if (c == null) {
                _Players.Remove(index);
                _Events.EmitPlayerUpdateEvent(index, null);
                return;
            }

            _Logger.LogInformation($"Loaded player {index}: {c.ID}/{c.Name}");

            TrackedPlayer player = new TrackedPlayer() {
                Character = c,
                ID = c.ID,
                CharacterName = c.Name,
                RunnerName = playerName ?? c.Name,
                Index = index
            };

            _Players[index] = player;

            _Realtime.Subscribe(new Subscription() {
                Characters = { c.ID },
                Events = { "Death" }
            });

            _Events.EmitPlayerUpdateEvent(index, player);
        }

        public void SetScore(int index, int score) {
            if (_Players.TryGetValue(index, out TrackedPlayer? player) == true) {
                player.Score = score;
            } else {
                _Logger.LogWarning($"Cannot set score of player {index}, _Players does not contain");
            }
        }

        public int GetScore(int index) {
            if (_Players.TryGetValue(index, out TrackedPlayer? player) == true) {
                return player.Score;
            } else {
                _Logger.LogWarning($"Cannot get score of player {index}, _Players does not contain");
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
            //_Logger.LogDebug($"Ticks: {nowTicks - prevTicks}");

            long startTicks = _MatchStart.Ticks;
            double ticksPerSecond = 10000000D;
            _Events.EmitTimerEvent((int) Math.Round((nowTicks - startTicks) / ticksPerSecond));

            _LastTimerTick = DateTime.UtcNow;
        }

        public void Start() {
            if (_State != MatchState.UNSTARTED) {
                _Logger.LogWarning($"Not starting match, already started");
                return;
            }

            _MatchTimer.AutoReset = true;
            _MatchTimer.Elapsed += OnTimerTick;
            _MatchTimer.Start();
            _LastTimerTick = DateTime.UtcNow;

            _MatchStart = DateTime.UtcNow;

            SetState(MatchState.RUNNING);
        }

        public void Restart() {
            _MatchStart = DateTime.UtcNow;
            _Events.EmitTimerEvent(0);
        }

        public void Reset() {
            _MatchStart = DateTime.UtcNow;
            _Events.EmitTimerEvent(0);
            _MatchTimer.Stop();

            SetState(MatchState.UNSTARTED);
        }

        public void Pause() {
            _MatchTimer.Stop();

            SetState(MatchState.PAUSED);
        }

        public void Stop() {
            _MatchTimer.Stop();
            _MatchEnd = DateTime.UtcNow;

            SetState(MatchState.FINISHED);
        }

        private void SetState(MatchState state) {
            if (_State == state) {
                return;
            }

            _State = state;
            _Events.EmitMatchStateEvent(_State);
        }

        public MatchState GetState() => _State;

        public DateTime GetMatchStart() => _MatchStart;

        public DateTime? GetMatchEnd() => null;

        public int GetMatchLength() {
            DateTime start = GetMatchStart();
            DateTime end = GetMatchEnd() ?? DateTime.UtcNow;

            TimeSpan startSpan = new TimeSpan(start.Ticks);
            TimeSpan endSpan = new TimeSpan(end.Ticks);

            return (int) (endSpan - startSpan).TotalSeconds;
        }

        public List<TrackedPlayer> GetPlayers() => _Players.Values.ToList();

        private void AddListeners() {
            _Events.OnKillEvent += KillHandler;
        }

        private void RemoveListeners() {
            _Events.OnKillEvent -= KillHandler;
        }

    }
}
