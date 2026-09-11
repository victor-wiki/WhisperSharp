using System.Collections;
using TorchSharp;

namespace WhisperSharp.Helper
{
    public class ArrayHelper
    {
        public static T[,] ToRectangular<T>(T[][] source)
        {
            int rows = source.Length;
            int cols = source.FirstOrDefault()?.Length??0;

            T[,] result = new T[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i, j] = source[i][j];
                }
            }

            return result;
        }

        private static Scalar[] GetScalars(object obj)
        {
            return (obj as ArrayList).ToArray().Select(item => item as Scalar).ToArray();            
        }

        public static Int32[] GetInt32Array(object obj)
        {
            return GetScalars(obj).Select(item => item.ToInt32()).ToArray();
        }

        public static Int64[] GetInt64Array(object obj)
        {
            return GetScalars(obj).Select(item => item.ToInt64()).ToArray();
        }

        public static float[] GetFloatArray(object obj)
        {
            return GetScalars(obj).Select(item => item.ToSingle()).ToArray();
        }

        public static double[] GetDoubleArray(object obj)
        {
            return GetScalars(obj).Select(item => item.ToDouble()).ToArray();
        }

        public static int[] GetIntArray(string str, char separator = ',')
        {
            return str.Split(separator).Select(item => int.Parse(item)).ToArray();
        }

        public static bool[] GetBoolArray(object obj)
        {
            return GetScalars(obj).Select(item => item.ToBoolean()).ToArray();
        }

        public static bool[] GetBoolListArray(object obj)
        {
            return (obj as List<object>).Select(item=> Convert.ToBoolean(item)).ToArray();
        }

        public static List<List<float>> GetFloatArrayList(object obj)
        {
            List<List<float>> result = new List<List<float>>();

            ArrayList arrayList = obj as ArrayList;

            foreach(var item in arrayList)
            {
                result.Add(GetScalars(item).Select(item => item.ToSingle()).ToList());
            }

            return result;
        }

        public static double[,] GetDouble2DimensonArray(object obj)
        {
            List<List<double>> result = new List<List<double>>();

            ArrayList arrayList = obj as ArrayList;

            foreach (var item in arrayList)
            {
                var subArrayList = item as ArrayList;

                List<double> subList = new List<double>();

                foreach(Scalar subItem in subArrayList)
                {
                    subList.Add(subItem.ToDouble());
                }

                result.Add(subList);
            }

            return ToRectangular<double>(result.Select(item=>item.ToArray()).ToArray());
        }

        public static T[] GetStepOnes<T>(IEnumerable<T> values, int step = 2)
        {
            var odds = values.Where((item, index) => index % step == 0);

            return odds.ToArray<T>();
        }

        public static int[] GetRange(int endNumber, int startNumber = 0)
        {
            int[] nums = new int[endNumber - startNumber];

            for (var i = startNumber; i < endNumber; i++)
            {
                nums[i - startNumber] = i;
            }

            return nums;
        }

        public static long[] GetRangeLong(long endNumber, long startNumber = 0)
        {
            long[] nums = new long[endNumber - startNumber];

            for (var i = startNumber; i < endNumber; i++)
            {
                nums[i - startNumber] = i;
            }

            return nums;
        }
    }
}
