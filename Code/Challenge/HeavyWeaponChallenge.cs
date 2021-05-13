using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models;
using watchtower.Models.Events;

namespace watchtower.Code.Challenge {

    public class HeavyWeaponChallenge : IRunChallenge {

        public int ID => 5;

        public string Name => "FACTION LOYALIST";

        public string Description => "Only faction specific heavy weapon kills count for the next 2 minutes";

        public int Multiplier => 2;

        public int Duration => 120;

        public ChallengeDurationType DurationType => ChallengeDurationType.TIMED;

        private const int CHAINGUN = 7533;
        private const int CHAINGUN_AE = 803779;
        private const int CHAINGUN_P = 6004168;
        private const int LASHER = 7540;
        private const int LASHER_AE = 803732;
        private const int LASHER_P = 6004144;
        private const int JACKHAMMER = 7528;
        private const int JACKHAMMER_AE = 803756;
        private const int JACKHAMMER_P = 6004174;

        public Task<bool> WasMet(KillEvent ev, PsItem? item) {
            return Task.FromResult(
                ev.WeaponID == CHAINGUN || ev.WeaponID == CHAINGUN_AE || ev.WeaponID == CHAINGUN_P
                || ev.WeaponID == LASHER || ev.WeaponID == LASHER_AE || ev.WeaponID == LASHER_P
                || ev.WeaponID == JACKHAMMER || ev.WeaponID == JACKHAMMER_AE || ev.WeaponID == JACKHAMMER_P
            );
        }
    }
}
