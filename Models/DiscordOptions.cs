namespace watchtower.Models {

    public class DiscordOptions {

        public bool Enabled { get; set; } = false;

        public ulong GuildId { get; set; }

        public ulong VoiceChannelId { get; set; }

        public ulong ParentChannelId { get; set; }

        public string ClientKey { get; set; } = "";

    }
}
