using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace watchtower.Services.Db.Patches {

    [PatchAttritube]
    public class Patch2CreateItemCategoryAndTypeTables : IDbPatch {

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection();
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS ps_item_type (
                    ID int4 NOT NULL PRIMARY KEY,
                    name varchar NOT NULL,
                    code varchar NOT NULL
                );

                CREATE TABLE IF NOT EXISTS ps_item_category (
                    ID int4 NOT NULL PRIMARY KEY,
                    name varchar NOT NULL
                );
            ");

            await cmd.ExecuteNonQueryAsync();
        }

        public int GetMinVersion() => 2;

        public string GetName() => "Create item category and type tables";

    }
}
