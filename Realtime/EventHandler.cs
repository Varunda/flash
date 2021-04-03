using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Census;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;
using watchtower.Services;

namespace watchtower.Realtime {

    public class EventHandler : IEventHandler {

        private readonly ILogger<EventHandler> _Logger;

        private readonly ICharacterCollection _Characters;
        private readonly IMatchManager _MatchManager;
        private readonly IEventBroadcastService _EventBroadcast;

        private readonly List<JToken> _Recent;

        public EventHandler(ILogger<EventHandler> logger,
            ICharacterCollection charCollection, IMatchManager matchManager,
            IEventBroadcastService eventBroadcast) {

            _Logger = logger;

            _Recent = new List<JToken>();

            _Characters = charCollection ?? throw new ArgumentNullException(nameof(charCollection));
            _EventBroadcast = eventBroadcast ?? throw new ArgumentNullException(nameof(eventBroadcast));
            _MatchManager = matchManager ?? throw new ArgumentNullException(nameof(matchManager));
        }

        public void Process(JToken ev) {
            if (_Recent.Contains(ev)) {
                _Logger.LogInformation($"Skipping duplicate event {ev}");
                return;
            }

            _Recent.Add(ev);
            if (_Recent.Count > 10) {
                _Recent.RemoveAt(0);
            }

            string? type = ev.Value<string?>("type");

            if (type == "serviceMessage") {
                JToken? payloadToken = ev.SelectToken("payload");
                if (payloadToken == null) {
                    _Logger.LogWarning($"Missing 'payload' from {ev}");
                    return;
                }

                string? eventName = payloadToken.Value<string?>("event_name");

                if (eventName == null) {
                    _Logger.LogWarning($"Missing 'event_name' from {ev}");
                } else if (eventName == "PlayerLogin") {
                    _ProcessPlayerLogin(payloadToken);
                } else if (eventName == "PlayerLogout") {
                    _ProcessPlayerLogout(payloadToken);
                } else if (eventName == "GainExperience") {
                    _ProcessExperience(payloadToken);
                } else if (eventName == "Death") {
                    _ProcessDeath(payloadToken);
                } else {
                    _Logger.LogWarning($"Untracked event_name: '{eventName}'");
                }
            }
        }

        private void _ProcessPlayerLogin(JToken payload) {
            string charID = payload.Value<string?>("character_id") ?? "";
        }

        private void _ProcessPlayerLogout(JToken payload) {
            string charID = payload.Value<string?>("character_id") ?? "";
        }

        private void _ProcessDeath(JToken payload) {
            DateTime timestamp = DateTimeOffset.FromUnixTimeMilliseconds((payload.Value<long?>("timestamp") ?? 0) * 1000).DateTime;

            string charID = payload.Value<string?>("character_id") ?? "0";
            string attackerID = payload.Value<string?>("attacker_character_id") ?? "0";

            string loadoutID = payload.Value<string?>("character_loadout_id") ?? "-1";
            string attackerLoadoutID = payload.Value<string?>("attacker_loadout_id") ?? "-1";

            _Characters.Cache(attackerID);
            _Characters.Cache(charID);

            KillEvent ev = new KillEvent() {
                SourceID = attackerID,
                TargetID = charID,
                LoadoutID = attackerLoadoutID,
                TargetLoadoutID = loadoutID,
                Timestamp = timestamp,
                IsHeadshot = (payload.Value<string?>("is_headshot") ?? "0") == "1",
                WeaponID = payload.Value<string?>("attacker_weapon_id") ?? "0",
                WorldID = payload.Value<string?>("world_id") ?? "0",
                ZoneID = payload.Value<string?>("zone_id") ?? "0"
            };

            //_Logger.LogInformation($"{payload}");

            _EventBroadcast.EmitKillEvent(ev);
        }

        private void _ProcessExperience(JToken payload) {
            //_Logger.LogInformation($"Processing exp: {payload}");

            string? charID = payload.Value<string?>("character_id");
            if (charID == null) {
                return;
            }
            _Characters.Cache(charID);

            string expId = payload.Value<string?>("experience_id") ?? "-1";
            string loadoutId = payload.Value<string?>("loadout_id") ?? "-1";
            int timestamp = payload.Value<int?>("timestamp") ?? 0;

            string factionID = Loadout.GetFaction(loadoutId);

            /*
            lock (CharacterStore.Get().Players) {
                TrackedPlayer p = CharacterStore.Get().Players.GetOrAdd(charID, new TrackedPlayer() {
                    ID = charID,
                    FactionID = factionID
                });

                if (expId == Experience.HEAL || expId == Experience.SQUAD_HEAL) {
                    p.Heals.Add(timestamp);
                } else if (expId == Experience.REVIVE || expId == Experience.SQUAD_REVIVE) {
                    p.Revives.Add(timestamp);

                    // Remove the most recent death
                    string? targetID = payload.Value<string?>("other_id");
                    if (targetID != null && targetID != "0") {
                        if (CharacterStore.Get().Players.TryGetValue(targetID, out TrackedPlayer? player) == true) {
                            if (player != null) {
                                if (player.Deaths.Count > 0) {
                                    player.Deaths.RemoveAt(player.Deaths.Count - 1);
                                }
                            }
                        }
                    }
                } else if (expId == Experience.RESUPPLY || expId == Experience.SQUAD_RESUPPLY) {
                    p.Resupplies.Add(timestamp);
                } else if (expId == Experience.MAX_REPAIR || expId == Experience.SQUAD_MAX_REPAIR) {
                    p.Resupplies.Add(timestamp);
                }
            }
            */
        }

    }
}
