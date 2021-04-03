using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models;

namespace watchtower.Constants {

    public static class ItemCategory {

        public static string Knife = "2";
        public static string Pistol = "3";
        public static string Shotgun = "4";
        public static string SMG = "5";
        public static string LMG = "6";
        public static string AssaultRifle = "7";
        public static string Carbine = "8";
        public static string MaxAvLeft = "9";
        public static string MaxAiLeft = "10";
        public static string Sniper = "11";
        public static string ScoutRifle = "12";
        public static string RocketLauncher = "13";
        public static string HeavyWeapon = "14";
        public static string FlakMax = "15";
        public static string Grenade = "17";
        public static string Explosive = "18";
        public static string BattleRifle = "19";
        public static string MaxAaRight = "20";
        public static string MaxAvRight = "21";
        public static string MaxAiRight = "22";
        public static string MaxAaLeft = "23";
        public static string Crossbow = "24";
        public static string Camo = "99";
        public static string Infantry = "100";
        public static string Vehicles = "101";
        public static string InfantryWeapons = "102";
        public static string InfantryGear = "103";
        public static string VehicleWeapons = "104";
        public static string VehicleGear = "105";
        public static string InfantryAbility = "139";
        public static string AerialCombatWeapon = "147";
        public static string HybridRifle = "157";
        public static string Weapon = "207";

        public static List<string> SpeedrunnerWeapons = new List<string>() {
            Knife, Pistol, Shotgun, SMG, LMG, AssaultRifle, Carbine, Sniper, BattleRifle,
            RocketLauncher, HeavyWeapon, Grenade, Explosive, Crossbow, InfantryAbility, InfantryWeapons,
            AerialCombatWeapon, HybridRifle
        };

        public static bool IsValidSpeedrunnerWeapon(PsItem item) {
            return SpeedrunnerWeapons.Contains(item.CategoryID);
        }

    }
}
