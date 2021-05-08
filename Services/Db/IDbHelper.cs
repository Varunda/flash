using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace watchtower.Services.Db {

    public interface IDbHelper {

        NpgsqlConnection Connection();

        Task<NpgsqlCommand> Command(NpgsqlConnection conn, string text);

    }

}
