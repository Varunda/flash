using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace watchtower.Models {

    /// <summary>
    /// Represents an entry from the item collection
    /// </summary>
    public class PsItem {

        /// <summary>
        /// ID of the entry
        /// </summary>
        public Int64 ItemID { get; set; }

        /// <summary>
        /// What type of item this entry is
        /// </summary>
        public int TypeID { get; set; }

        /// <summary>
        /// What category this entry is
        /// </summary>
        public int CategoryID { get; set; }

        /// <summary>
        /// Name of this item
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Faction the item belongs to
        /// </summary>
        public int FactionID { get; set; }

    }

}
