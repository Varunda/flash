using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using watchtower.Models;
using watchtower.Services;

namespace watchtower.Controllers {

    [ApiController]
    [Route("/logs")]
    public class LogDownloadController : ControllerBase {

        private readonly IMatchMessageBroadcastService _MatchLog;
        private readonly IAdminMessageBroadcastService _AdminLog;

        public LogDownloadController(IMatchMessageBroadcastService matchLog,
            IAdminMessageBroadcastService adminLog) {

            _MatchLog = matchLog;
            _AdminLog = adminLog;
        }

        [HttpGet("match")]
        public FileResult Match() {
            List<Message> messages = _MatchLog.GetMessages();

            string file = $"Match logs generated at {DateTime.UtcNow} UTC\n";

            foreach (Message msg in messages) {
                file += $"{msg.Timestamp} >> {msg.Content}\n";
            }

            return File(Encoding.UTF8.GetBytes(file), "text/plain", "MatchLogs.txt");
        }

        [HttpGet("admin")]
        public FileResult Admin() {
            List<Message> messages = _AdminLog.GetMessages();

            string file = $"Match logs generated at {DateTime.UtcNow} UTC\n";

            foreach (Message msg in messages) {
                file += $"{msg.Timestamp} >> {msg.Content}\n";
            }

            return File(Encoding.UTF8.GetBytes(file), "text/plain", "MatchLogs.txt");
        }

    }
}
