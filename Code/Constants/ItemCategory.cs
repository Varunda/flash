using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models;

namespace watchtower.Constants {

    public static class ItemCategory {

        public static int Knife = 2;
        public static int Pistol = 3;
        public static int Shotgun = 4;
        public static int SMG = 5;
        public static int LMG = 6;
        public static int AssaultRifle = 7;
        public static int Carbine = 8;
        public static int MaxAvLeft = 9;
        public static int MaxAiLeft = 10;
        public static int Sniper = 11;
        public static int ScoutRifle = 12;
        public static int RocketLauncher = 13;
        public static int HeavyWeapon = 14;
        public static int FlakMax = 15;
        public static int Grenade = 17;
        public static int Explosive = 18;
        public static int BattleRifle = 19;
        public static int MaxAaRight = 20;
        public static int MaxAvRight = 21;
        public static int MaxAiRight = 22;
        public static int MaxAaLeft = 23;
        public static int Crossbow = 24;
        public static int Camo = 99;
        public static int Infantry = 100;
        public static int Vehicles = 101;
        public static int InfantryWeapons = 102;
        public static int InfantryGear = 103;
        public static int VehicleWeapons = 104;
        public static int VehicleGear = 105;
        public static int InfantryAbility = 139;
        public static int AerialCombatWeapon = 147;
        public static int HybridRifle = 157;
        public static int Weapon = 207;

        public static List<int> SpeedrunnerWeapons = new List<int>() {
            Knife, Pistol, Shotgun, SMG, LMG, AssaultRifle, Carbine, Sniper, BattleRifle, ScoutRifle,
            RocketLauncher, HeavyWeapon, Grenade, Explosive, Crossbow, InfantryAbility, InfantryWeapons,
            AerialCombatWeapon, HybridRifle
        };

        public static bool IsValidSpeedrunnerWeapon(PsItem item) {
            return SpeedrunnerWeapons.Contains(item.CategoryID);
        }

    }
}
