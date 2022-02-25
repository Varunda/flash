using System;

namespace watchtower.Code {

    public static class DateTimeExtensionMethods {

        /// <summary>
        /// Get the Discord relative timestamp format, very useful for Discord messages!
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string GetDiscordFormat(this DateTime time) {
            DateTimeOffset offset = new DateTimeOffset(time);
            return $"<t:{offset.ToUnixTimeSeconds()}:f>";
        }

    }
}
