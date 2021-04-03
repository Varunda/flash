using DaybreakGames.Census;
using DaybreakGames.Census.Exceptions;
using DaybreakGames.Census.Operators;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models;

namespace watchtower.Census {

    public class ItemCollection : IItemCollection {

        private readonly ILogger<ItemCollection> _Logger;

        private readonly ICensusQueryFactory _Census;

        private readonly ConcurrentDictionary<string, PsItem?> _Cache = new ConcurrentDictionary<string, PsItem?>();

        public ItemCollection(ILogger<ItemCollection> logger,
            ICensusQueryFactory query) {

            _Logger = logger;
            _Census = query;
        }

        public async Task<PsItem?> GetByIDAsync(string ID) {
            PsItem? item = null;
            lock (_Cache) {
                if (_Cache.TryGetValue(ID, out item)) {
                    return item;
                }
            }

            item = await _GetFromCensus(ID, true);

            lock (_Cache) {
                _Cache.TryAdd(ID, item);
            }

            return item;
        }

        private async Task<PsItem?> _GetFromCensus(string ID, bool retry) {
            CensusQuery query = _Census.Create("item");

            query.Where("item_id").Equals(ID);

            try {
                JToken result = await query.GetAsync();

                PsItem? player = _ParseItem(result);

                return player;
            } catch (CensusConnectionException ex) {
                if (retry == true) {
                    _Logger.LogWarning("Retrying Item {Item} from API", ID);
                    return await _GetFromCensus(ID, false); 
                } else {
                    _Logger.LogError(ex, "Failed to get Item {0} from API", ID);
                    throw ex;
                }
            }
        }

        private PsItem? _ParseItem(JToken result) {
            if (result == null) {
                return null;
            }

            //_Logger.LogInformation($"{result}");

            PsItem item = new PsItem() {
                ItemID = result.Value<long?>("item_id") ?? 0,
                TypeID = result.Value<string?>("item_type_id") ?? "0",
                CategoryID = result.Value<string?>("item_category_id") ?? "0",
                FactionID = result.Value<string?>("faction_id") ?? "0"
            };

            JToken? name = result.SelectToken("name");
            if (name == null) {
                _Logger.LogWarning($"Missing name token in {result}");
            } else {
                item.Name = name.Value<string?>("en") ?? "<MISSING NAME>";
            }

            return item;
        }

    }

}
