using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Census;
using watchtower.Constants;
using watchtower.Models;
using watchtower.Services;

namespace watchtower.Commands {

    [Command]
    public class StoreCommand {

        private readonly ILogger<StoreCommand> _Logger;
        private readonly ICharacterCollection _Characters;

        public StoreCommand(IServiceProvider services) {
            _Logger = (ILogger<StoreCommand>)services.GetService(typeof(ILogger<StoreCommand>));

            _Characters = (ICharacterCollection)services.GetService(typeof(ICharacterCollection));
        }

        public async Task Print(string nameOrId) {
            Character? c;
            if (nameOrId.Length == 19) {
                c = await _Characters.GetByIDAsync(nameOrId);
            } else {
                c = await _Characters.GetByNameAsync(nameOrId);
            }

            if (c == null) {
                _Logger.LogWarning($"Failed to find {nameOrId}");
                return;
            }

            TrackedPlayer? player;
            bool found;
            lock (CharacterStore.Get().Players) {
                found = CharacterStore.Get().Players.TryGetValue(c.ID, out player);
            }

            if (found == false || player == null) {
                _Logger.LogWarning($"{c.Name}/{c.ID} not tracked");
                return;
            }

            _Logger.LogInformation(
                $"Character {nameOrId}:\n"
                + $"\tName: {c.Name}\n"
                + $"\tID: {c.ID}\n"
                + $"\tFactionID: {c.FactionID}\n"
                + $"\tKills: {player.Kills.Count}\n"
                + $"\tDeaths: {player.Deaths.Count}\n"
            );
        }

        public void Outfit(string tag) {
            Dictionary<string, TrackedPlayer> players;
            lock (CharacterStore.Get().Players) {
                players = new Dictionary<string, TrackedPlayer>(CharacterStore.Get().Players);
            }

            List<Character> characters = new List<Character>(_Characters.GetCache());

            List<string> charIDs = new List<string>();

            foreach (Character c in characters) {
                if (c.OutfitTag?.ToLower() == tag.ToLower()) {
                    charIDs.Add(c.ID);
                }
            }

            int online = 0;
            int total = 0;
            int kills = 0;
            int killsOnline = 0;
            int deaths = 0;
            int deathsOnline = 0;

            foreach (string charID in charIDs) {
                if (players.TryGetValue(charID, out TrackedPlayer? p) == true) {
                    if (p == null) {
                        continue;
                    }

                    ++total;
                    kills += p.Kills.Count;
                    deaths += p.Deaths.Count;
                }
            }

            _Logger.LogInformation(
                $"Outfit: {tag}\n"
                + $"\tOnline: {online} / Total: {total}\n"
                + $"\tKills: {kills} / {killsOnline}\n"
                + $"\tDeaths: {deaths} / {deathsOnline}\n"
            );
        }

    }
}
