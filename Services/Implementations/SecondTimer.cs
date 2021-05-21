using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using watchtower.Models.Events;

namespace watchtower.Services.Implementations {

    public class SecondTimer : ISecondTimer, IDisposable {

        private readonly Timer _Timer;
        private DateTime _LastTick;

        private long _TotalTicks = 0;
        private DateTime _Start = DateTime.UtcNow;

        const double TICKS_PER_SECOND = 10000000D;

        public SecondTimer() {
            _LastTick = DateTime.UtcNow;

            _Timer = new Timer {
                AutoReset = true,
                Interval = 1000
            };
            _Timer.Elapsed += _OnTick;
            _Timer.Start();
        }

        public event EventHandler<SecondTimerArgs>? OnTick;

        private void _OnTick(object? sender, ElapsedEventArgs args) {
            DateTime time = args.SignalTime.ToUniversalTime();

            long nowTicks = time.Ticks;
            long prevTicks = _LastTick.Ticks;

            _TotalTicks += nowTicks - prevTicks;

            //_Logger.LogDebug($"Total ticks: {_MatchTicks}, seconds {Math.Round(_MatchTicks / TICKS_PER_SECOND)}");

            _LastTick = DateTime.UtcNow;

            OnTick?.Invoke(this, new SecondTimerArgs() {
                Seconds = (int)Math.Round(_TotalTicks / TICKS_PER_SECOND),
                TotalTicks = _TotalTicks,
                ElapsedTicks = nowTicks - prevTicks,
                Start = _Start
            });
        }

        public void Dispose() {
            _Timer.Elapsed -= _OnTick;
            _Timer.Dispose();
        }

    }
}
