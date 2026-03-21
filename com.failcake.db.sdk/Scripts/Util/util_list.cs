#region

using System;
using System.Collections.Generic;

#endregion

namespace HyenaQuest
{
    public static class util_list
    {
        private static Random rng = new Random();

        public static void SetSeed(int seed) {
            util_list.rng = seed == 0 ? new Random() : new Random(seed);
        }

        public static IList<T> Shuffle<T>(this IList<T> list) {
            if (list == null) return null;

            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = util_list.rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }

            return list;
        }

        public static IList<T> ShuffleExcept<T>(this IList<T> list, int index) {
            if (list == null) return null;

            int n = list.Count;
            if (index < 0 || index >= n) return list;

            for (int i = n - 1; i > 0; i--)
            {
                if (i == index) continue;

                int k = util_list.rng.Next(i + 1);
                if (k == index) k = i;

                (list[k], list[i]) = (list[i], list[k]);
            }

            return list;
        }

        public static IList<T> ShuffleWithNew<T>(this IList<T> list) {
            if (list == null) return null;

            List<T> newList = new List<T>(list);
            int n = newList.Count;
            while (n > 1)
            {
                n--;
                int k = util_list.rng.Next(n + 1);
                (newList[k], newList[n]) = (newList[n], newList[k]);
            }

            return newList;
        }

        public static void Shuffle<T>(this HashSet<T> set) {
            if (set == null || set.Count == 0) return;

            List<T> list = new List<T>(set);
            list.Shuffle();
            set.Clear();
            foreach (T item in list) set.Add(item);
        }
    }
}