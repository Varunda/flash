using DaybreakGames.Census;
using DaybreakGames.Census.Operators;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models.Census;

namespace watchtower.Code.Census.Implementations {

    public class ExperienceCollection {

        private readonly ILogger<ExperienceCollection> _Logger;
        private readonly ICensusQueryFactory _Census;

        private readonly Dictionary<int, ExpEntry> _Entries = new Dictionary<int, ExpEntry>();

        public ExperienceCollection(ILogger<ExperienceCollection> logger,
            ICensusQueryFactory census) {

            _Logger = logger;
            _Census = census;
        }

        public async Task<List<ExpEntry>> GetAll() {
            if (_Entries.Count > 0) {
                return _Entries.Values.ToList();
            }

            CensusQuery query = _Census.Create("experience");
            query.SetLimit(10_000);

            List<ExpEntry> entries = new List<ExpEntry>();

            try {
                IEnumerable<JToken> tokens = await query.GetListAsync();

                foreach (JToken token in tokens) {
                    ExpEntry entry = new ExpEntry() {
                        ID = token.GetInt32("experience_id", 0),
                        Description = token.GetString("description", "")
                    };
                    entries.Add(entry);

                    lock (_Entries) {
                        if (_Entries.ContainsKey(entry.ID) == false) {
                            _Entries.Add(entry.ID, entry);
                        }
                    }
                }
            } catch (Exception ex) {
                _Logger.LogError(ex.Message);
            }

            return entries;
        }

        public async Task<ExpEntry?> GetByID(int id) {
            if (_Entries.Count > 0) {
                _ = _Entries.TryGetValue(id, out ExpEntry? entry);
                return entry;
            }

            _ = await GetAll();
            _ = _Entries.TryGetValue(id, out ExpEntry? entry2);
            return entry2;
        }

    }
}
