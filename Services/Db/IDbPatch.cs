using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace watchtower.Services.Db {

    public interface IDbPatch {

        /// <summary>
        /// If the current version of the patch is below this version, execute the patch
        /// </summary>
        int GetMinVersion();

        /// <summary>
        /// Get a display friendly name of the patch
        /// </summary>
        /// <returns></returns>
        string GetName();

        /// <summary>
        /// Perform the patch
        /// </summary>
        Task Execute(IDbHelper helper);

    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PatchAttritube : Attribute {
    
    }

}
