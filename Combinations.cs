using System.Collections.Generic;

namespace Samana.Algorithms
{

    public static class Combinations
    {

        // Возвращает список коллекций со всеми возможными вариантами перестановок в source
        public static List<T[]> AllCombinations<T>(T[] source)
        {
            List<T[]> resultList = new List<T[]>();
            fooRecursion(source, resultList);
            return resultList;


            void fooRecursion(T[] originalSource, List<T[]> result, List<int> currentSet = null)
            {
                if (currentSet == null) currentSet = new List<int>();

                for (int i = 0; i < originalSource.Length; i++)
                {
                    if (!currentSet.Contains(i))
                    {
                        List<int> cloneSet = new List<int>(currentSet);
                        cloneSet.Add(i);
                        if (cloneSet.Count == originalSource.Length)
                        {
                            T[] r = new T[originalSource.Length];
                            for (int j = 0; j < cloneSet.Count; j++)
                            {
                                r[j] = originalSource[cloneSet[j]];
                            }
                            result.Add(r);
                            return;
                        }
                        else
                            fooRecursion(originalSource, result, cloneSet);
                    }
                    else continue;
                }
            }
        }
    }
}
