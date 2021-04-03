using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models;

namespace watchtower.Census {

    public interface IItemCollection {

        /// <summary>
        /// Get a specific <see cref="PsItem"/> by its ID
        /// </summary>
        /// <param name="ID">ID of the item to get</param>
        /// <returns>The <see cref="PsItem"/> with <see cref="PsItem.ItemID"/> of <paramref name="ID"/>,
        ///     or <c>null</c> if it doesn't exist</returns>
        Task<PsItem?> GetByIDAsync(string ID);

    }

}
