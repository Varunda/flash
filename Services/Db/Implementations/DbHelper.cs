using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models;

namespace watchtower.Services.Db {

    public class DbHelper : IDbHelper {

        private readonly ILogger<DbHelper> _Logger;

        private readonly DbOptions _DbOptions;

        public DbHelper(ILogger<DbHelper> logger, IOptions<DbOptions> options) {
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _DbOptions = options.Value;
        }

        public NpgsqlConnection Connection() {
            string connStr = $"Host={_DbOptions.ServerUrl};Username={_DbOptions.Username};Password={_DbOptions.Password};Database={_DbOptions.DatabaseName}";

            NpgsqlConnection conn = new NpgsqlConnection(connStr);

            return conn;
        }

        public async Task<NpgsqlCommand> Command(NpgsqlConnection conn, string text) {
            await conn.OpenAsync();

            NpgsqlCommand cmd = conn.CreateCommand();
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.CommandText = text;

            return cmd;
        }

    }
}
