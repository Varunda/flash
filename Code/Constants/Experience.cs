using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace watchtower.Constants {

    public sealed class Experience {

        public static int HEAL = 4;
        public static int MAX_REPAIR = 6;
        public static int REVIVE = 7;
        public static int RESUPPLY = 34;
        public static int SQUAD_HEAL = 51;
        public static int SQUAD_REVIVE = 53;
        public static int SQUAD_RESUPPLY = 55;
        public static int SQUAD_MAX_REPAIR = 142;

        public static int KILL = 1;
        public static int NEMESIS_KILL = 32;
        public static int SPAWN_KILL = 277;
        public static int PRIORITY_KILL = 278;
        public static int HIGH_PRIORITY_KILL = 279;
        
        public static bool IsKill(int expID) {
            return expID == KILL || expID == NEMESIS_KILL || expID == SPAWN_KILL 
                || expID == PRIORITY_KILL || expID == HIGH_PRIORITY_KILL;
        }

        public static int ASSIST = 2;
        public static int SPAWN_ASSIST = 3;
        public static int PRIORITY_ASSIST = 371;
        public static int HIGH_PRIORITY_ASSIST = 372;

        public static bool IsAssist(int expID) {
            return expID == ASSIST || expID == SPAWN_ASSIST
                || expID == PRIORITY_ASSIST || expID == HIGH_PRIORITY_ASSIST;
        }

    }
}
