using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace watchtower.Services.Db.Patches {

    [PatchAttritube]
    public class Patch1CreateItemTable : IDbPatch {

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection();
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS ps_item (
                    ID int4 NOT NULL PRIMARY KEY,
                    name varchar NOT NULL,
                    type_id int4 NOT NULL,
                    category_id int4 NOT NULL,
                    faction_id int4 NOT NULL
                );
            ");

            await cmd.ExecuteNonQueryAsync();
        }

        public int GetMinVersion() => 1;

        public string GetName() => "Create item table";

    }
}
