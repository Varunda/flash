using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace watchtower.Models {

    /// <summary>
    /// Options used to connect to the database
    /// </summary>
    public class DbOptions {

        /// <summary>
        /// URL of the PSQL server
        /// </summary>
        public string ServerUrl { get; set; } = "";

        /// <summary>
        /// Database name to use
        /// </summary>
        public string DatabaseName { get; set; } = "";

        /// <summary>
        /// Username to connect to the database with
        /// </summary>
        public string Username { get; set; } = "";

        /// <summary>
        /// Password to connect to the database with
        /// </summary>
        public string Password { get; set; } = "";

    }
}
