using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Census;
using watchtower.Code;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Models.Events;
using watchtower.Services;

namespace watchtower.Realtime {

    public class EventHandler : IEventHandler {

        private readonly ILogger<EventHandler> _Logger;

        private readonly ICharacterCollection _Characters;
        private readonly IRealtimeEventBroadcastService _EventBroadcast;

        private readonly List<JToken> _Recent;

        public EventHandler(ILogger<EventHandler> logger,
            ICharacterCollection charCollection,
            IRealtimeEventBroadcastService eventBroadcast) {

            _Logger = logger;

            _Recent = new List<JToken>();

            _Characters = charCollection ?? throw new ArgumentNullException(nameof(charCollection));
            _EventBroadcast = eventBroadcast ?? throw new ArgumentNullException(nameof(eventBroadcast));
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
            } else if (type == "heartbeat") {

            }
        }

        private void _ProcessPlayerLogin(JToken payload) {
            string charID = payload.Value<string?>("character_id") ?? "";
        }

        private void _ProcessPlayerLogout(JToken payload) {
            string charID = payload.Value<string?>("character_id") ?? "";
        }

        private void _ProcessDeath(JToken payload) {
            string charID = payload.Value<string?>("character_id") ?? "0";
            string attackerID = payload.Value<string?>("attacker_character_id") ?? "0";

            _Characters.Cache(attackerID);
            _Characters.Cache(charID);

            KillEvent ev = new KillEvent() {
                SourceID = attackerID,
                TargetID = charID,
                LoadoutID = payload.GetInt32("character_loadout_id", -1),
                TargetLoadoutID = payload.GetInt32("attacker_loadout_id", -1),
                Timestamp = payload.CensusTimestamp("timestamp"),
                IsHeadshot = (payload.Value<string?>("is_headshot") ?? "0") == "1",
                WeaponID = payload.GetInt32("attacker_weapon_id", 0),
                WorldID = payload.GetInt32("world_id", -1),
                ZoneID = payload.GetInt32("zone_id", -1)
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

            ExpEvent ev = new ExpEvent() {
                Amount = payload.GetInt32("amount", -1),
                ExpID = payload.GetInt32("experience_id", 0),
                LoadoutID = payload.GetInt32("loadout_id", -1),
                SourceID = payload.GetString("character_id", "0"),
                TargetID = payload.GetString("other_id", "0"),
                Timestamp = payload.CensusTimestamp("timestamp"),
                WorldID = payload.GetInt32("world_id", -1),
                ZoneID = payload.GetInt32("zone_id", -1)
            };

            _EventBroadcast.EmitExpEvent(ev);
        }

    }
}
