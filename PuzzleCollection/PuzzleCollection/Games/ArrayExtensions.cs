using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games
{
    public static class ArrayExtensions
    {
        public static void Shuffle<T>(this T[] array, int nelts, Random rs)
        {
            int i;
            for (i = nelts; i-- > 1 ;) {
                int j = rs.Next(0, i + 1); //random_upto(rs, i + 1);
                if (j != i)
                {
                    var temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;
                }
            }
        }

        public static int CompareTo<T>(this T[] array, int offset, T[] other, int otherOffset, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            for (int i = 0; i < length; ++i)
            {
                var a = array[offset + i];
                var b = other[otherOffset + i];
                if (!object.Equals(a,b))
                {
                    return 1;
                }
            }
            return 0;
        }

        public static ArraySegment<T> Segment<T>(this T[] array, int offset)
        {
            return new ArraySegment<T>(array, offset, array.Length - offset);
        }
        public static void SetAll<T>(this T[] array, T value)
        {
            for (int i = 0; i < array.Length; ++i)
            {
                array[i] = value;
            }
        }
    }
}
