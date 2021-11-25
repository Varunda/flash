using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace watchtower.Constants {

    public sealed class Loadout {

        public static int NC_INFILTRATOR = 1;
        public static int NC_LIGHT_ASSAULT = 3;
        public static int NC_MEDIC = 4;
        public static int NC_ENGINEER = 5;
        public static int NC_HEAVY_ASSAULT = 6;
        public static int NC_MAX = 7;

        public static int TR_INFILTRATOR = 8;
        public static int TR_LIGHT_ASSAULT = 10;
        public static int TR_MEDIC = 11;
        public static int TR_ENGINEER = 12;
        public static int TR_HEAVY_ASSAULT = 13;
        public static int TR_MAX = 14;

        public static int VS_INFILTRATOR = 15;
        public static int VS_LIGHT_ASSAULT = 17;
        public static int VS_MEDIC = 18;
        public static int VS_ENGINEER = 19;
        public static int VS_HEAVY_ASSAULT = 20;
        public static int VS_MAX = 21;

        public static string GetFaction(int loadoutID) {
            if (loadoutID == NC_INFILTRATOR
                    || loadoutID == NC_LIGHT_ASSAULT
                    || loadoutID == NC_MEDIC 
                    || loadoutID == NC_ENGINEER 
                    || loadoutID == NC_HEAVY_ASSAULT
                    || loadoutID == NC_MAX) {
                return Factions.NC;
            }
            if (loadoutID == VS_INFILTRATOR
                    || loadoutID == VS_LIGHT_ASSAULT
                    || loadoutID == VS_MEDIC 
                    || loadoutID == VS_ENGINEER 
                    || loadoutID == VS_HEAVY_ASSAULT
                    || loadoutID == VS_MAX) {
                return Factions.VS;
            }
            if (loadoutID == TR_INFILTRATOR
                    || loadoutID == TR_LIGHT_ASSAULT
                    || loadoutID == TR_MEDIC 
                    || loadoutID == TR_ENGINEER 
                    || loadoutID == TR_HEAVY_ASSAULT
                    || loadoutID == TR_MAX) {
                return Factions.TR;
            }

            return Factions.UNKNOWN;
        }

        public static bool IsTR(int loadoutID) {
            return loadoutID == TR_INFILTRATOR
                || loadoutID == TR_LIGHT_ASSAULT
                || loadoutID == TR_MEDIC
                || loadoutID == TR_ENGINEER
                || loadoutID == TR_HEAVY_ASSAULT
                || loadoutID == TR_MAX;
        }

        public static bool IsNC(int loadoutID) {
            return loadoutID == NC_INFILTRATOR
                || loadoutID == NC_LIGHT_ASSAULT
                || loadoutID == NC_MEDIC
                || loadoutID == NC_ENGINEER
                || loadoutID == NC_HEAVY_ASSAULT
                || loadoutID == NC_MAX;
        }

        public static bool IsVS(int loadoutID) {
            return loadoutID == VS_INFILTRATOR
                || loadoutID == VS_LIGHT_ASSAULT
                || loadoutID == VS_MEDIC
                || loadoutID == VS_ENGINEER
                || loadoutID == VS_HEAVY_ASSAULT
                || loadoutID == VS_MAX;
        }

    }
}
