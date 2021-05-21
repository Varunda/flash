using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace watchtower.Code {

    public static class ListExtensionMethods {

        private static Random _Random = new Random();

        /// <summary>
        /// Random shuffle of a List
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inst"></param>
        /// <returns></returns>
        public static List<T> Shuffle<T>(this List<T> inst) {
            List<T> list = new List<T>(inst);

            int n = list.Count;

            while (n > 1) {
                n--;
                int k = _Random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }

            return list;
        }

    }
}
