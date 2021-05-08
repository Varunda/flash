using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace watchtower.Code {

    public static class JTokenExtensionMethods {

        /// <summary>
        /// Get a string value from a <see cref="JToken"/>
        /// </summary>
        /// <param name="token">Extension token</param>
        /// <param name="name">Name of the value to get from <paramref name="token"/></param>
        /// <param name="def">Default fallback value if no value is present</param>
        /// <returns>The string value from <paramref name="token"/> in the field named <paramref name="name"/>,
        ///     or the value passed in <paramref name="def"/> if that field is empty or does not exist</returns>
        public static string GetString(this JToken token, string name, string def) {
            return token.Value<string?>(name) ?? def;
        } 

        public static int GetInt32(this JToken token, string name, int def) {
            return token.Value<int?>(name) ?? def;
        }

        /// <summary>
        /// Get a nullable string value from a <see cref="JToken"/>
        /// </summary>
        /// <param name="ext"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string? NullableString(this JToken ext, string name) {
            return ext.Value<string?>(name);
        }

        /// <summary>
        /// Get the corresponding <see cref="DateTime"/> from the seconds epoch Census uses
        /// </summary>
        /// <param name="ext"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static DateTime CensusTimestamp(this JToken ext, string field) {
            return DateTimeOffset.FromUnixTimeSeconds(ext.Value<int?>(field) ?? 0).UtcDateTime;
        }

    }
}
