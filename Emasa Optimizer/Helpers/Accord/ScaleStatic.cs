using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emasa_Optimizer.Helpers.Accord
{
    public static class ScaleStatic
    {
        #region Scaling functions
        /// <summary>
        ///   Converts the value x (which is measured in the scale
        ///   'from') to another value measured in the scale 'to'.
        /// </summary>
        /// 
        public static int Scale(this IntRange from, IntRange to, int x)
        {
            return VecScale.Scale(x, (IRange<int>)from, (IRange<int>)to);
        }

        /// <summary>
        ///   Converts the value x (which is measured in the scale
        ///   'from') to another value measured in the scale 'to'.
        /// </summary>
        /// 
        public static double Scale(this DoubleRange from, DoubleRange to, double x)
        {
            return VecScale.Scale(x, from, to);
        }

        /// <summary>
        ///   Converts the value x (which is measured in the scale
        ///   'from') to another value measured in the scale 'to'.
        /// </summary>
        /// 
        public static double Scale(double fromMin, double fromMax, double toMin, double toMax, double x)
        {
            return VecScale.Scale(x, fromMin, fromMax, toMin, toMax);
        }

        /// <summary>
        ///   Converts the value x (which is measured in the scale
        ///   'from') to another value measured in the scale 'to'.
        /// </summary>
        /// 
        public static float Scale(float fromMin, float fromMax, float toMin, float toMax, float x)
        {
            return VecScale.Scale(x, fromMin, fromMax, toMin, toMax);
        }

        /// <summary>
        ///   Converts the value x (which is measured in the scale
        ///   'from') to another value measured in the scale 'to'.
        /// </summary>
        /// 
        public static double Scale(IntRange from, DoubleRange to, int x)
        {
            return VecScale.Scale(x, from, to);
        }


        #endregion

    }

    public static class VecScale
    {
        #region Scale
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static int Scale(this int value, int fromMin, int fromMax, int toMin, int toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (int)value;
            return (int)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this int[] values, int fromMin, int fromMax, int toMin, int toMax, int[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                return values;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (int)((toMax - toMin) * (values[i] - fromMin) / (int)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int Scale(this int value, IRange<int> fromRange, IRange<int> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this int[] values, int fromMin, int fromMax, int toMin, int toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new int[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this int[] values, int toMin, int toMax)
        {
            int[] result = new int[values.Length];
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this int[] values, int toMin, int toMax, int[] result)
        {
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this int[] values, IRange<int> fromRange, IRange<int> toRange, int[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this int[] values, IRange<int> fromRange, IRange<int> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new int[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this int[] values, IRange<int> toRange, int[] result)
        {
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static float Scale(this int value, int fromMin, int fromMax, float toMin, float toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (float)value;
            return (float)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this int[] values, int fromMin, int fromMax, float toMin, float toMax, float[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (float)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (float)((toMax - toMin) * (values[i] - fromMin) / (float)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float Scale(this int value, IRange<int> fromRange, IRange<float> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this int[] values, int fromMin, int fromMax, float toMin, float toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new float[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this int[] values, float toMin, float toMax)
        {
            float[] result = new float[values.Length];
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this int[] values, float toMin, float toMax, float[] result)
        {
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this int[] values, IRange<int> fromRange, IRange<float> toRange, float[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this int[] values, IRange<int> fromRange, IRange<float> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new float[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this int[] values, IRange<float> toRange, float[] result)
        {
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static double Scale(this int value, int fromMin, int fromMax, double toMin, double toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (double)value;
            return (double)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this int[] values, int fromMin, int fromMax, double toMin, double toMax, double[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (double)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (double)((toMax - toMin) * (values[i] - fromMin) / (double)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double Scale(this int value, IRange<int> fromRange, IRange<double> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this int[] values, int fromMin, int fromMax, double toMin, double toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new double[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this int[] values, double toMin, double toMax)
        {
            double[] result = new double[values.Length];
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this int[] values, double toMin, double toMax, double[] result)
        {
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this int[] values, IRange<int> fromRange, IRange<double> toRange, double[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this int[] values, IRange<int> fromRange, IRange<double> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new double[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this int[] values, IRange<double> toRange, double[] result)
        {
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static short Scale(this int value, int fromMin, int fromMax, short toMin, short toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (short)value;
            return (short)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this int[] values, int fromMin, int fromMax, short toMin, short toMax, short[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (short)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (short)((toMax - toMin) * (values[i] - fromMin) / (short)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short Scale(this int value, IRange<int> fromRange, IRange<short> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this int[] values, int fromMin, int fromMax, short toMin, short toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new short[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this int[] values, short toMin, short toMax)
        {
            short[] result = new short[values.Length];
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this int[] values, short toMin, short toMax, short[] result)
        {
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this int[] values, IRange<int> fromRange, IRange<short> toRange, short[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this int[] values, IRange<int> fromRange, IRange<short> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new short[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this int[] values, IRange<short> toRange, short[] result)
        {
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static byte Scale(this int value, int fromMin, int fromMax, byte toMin, byte toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (byte)value;
            return (byte)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this int[] values, int fromMin, int fromMax, byte toMin, byte toMax, byte[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (byte)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (byte)((toMax - toMin) * (values[i] - fromMin) / (byte)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte Scale(this int value, IRange<int> fromRange, IRange<byte> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this int[] values, int fromMin, int fromMax, byte toMin, byte toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new byte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this int[] values, byte toMin, byte toMax)
        {
            byte[] result = new byte[values.Length];
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this int[] values, byte toMin, byte toMax, byte[] result)
        {
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this int[] values, IRange<int> fromRange, IRange<byte> toRange, byte[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this int[] values, IRange<int> fromRange, IRange<byte> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new byte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this int[] values, IRange<byte> toRange, byte[] result)
        {
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte Scale(this int value, int fromMin, int fromMax, sbyte toMin, sbyte toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (sbyte)value;
            return (sbyte)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this int[] values, int fromMin, int fromMax, sbyte toMin, sbyte toMax, sbyte[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (sbyte)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (sbyte)((toMax - toMin) * (values[i] - fromMin) / (sbyte)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte Scale(this int value, IRange<int> fromRange, IRange<sbyte> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this int[] values, int fromMin, int fromMax, sbyte toMin, sbyte toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new sbyte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this int[] values, sbyte toMin, sbyte toMax)
        {
            sbyte[] result = new sbyte[values.Length];
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this int[] values, sbyte toMin, sbyte toMax, sbyte[] result)
        {
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this int[] values, IRange<int> fromRange, IRange<sbyte> toRange, sbyte[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this int[] values, IRange<int> fromRange, IRange<sbyte> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new sbyte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this int[] values, IRange<sbyte> toRange, sbyte[] result)
        {
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static long Scale(this int value, int fromMin, int fromMax, long toMin, long toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (long)value;
            return (long)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this int[] values, int fromMin, int fromMax, long toMin, long toMax, long[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (long)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (long)((toMax - toMin) * (values[i] - fromMin) / (long)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long Scale(this int value, IRange<int> fromRange, IRange<long> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this int[] values, int fromMin, int fromMax, long toMin, long toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new long[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this int[] values, long toMin, long toMax)
        {
            long[] result = new long[values.Length];
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this int[] values, long toMin, long toMax, long[] result)
        {
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this int[] values, IRange<int> fromRange, IRange<long> toRange, long[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this int[] values, IRange<int> fromRange, IRange<long> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new long[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this int[] values, IRange<long> toRange, long[] result)
        {
            int fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static int Scale(this float value, float fromMin, float fromMax, int toMin, int toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (int)value;
            return (int)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this float[] values, float fromMin, float fromMax, int toMin, int toMax, int[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (int)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (int)((toMax - toMin) * (values[i] - fromMin) / (int)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int Scale(this float value, IRange<float> fromRange, IRange<int> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this float[] values, float fromMin, float fromMax, int toMin, int toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new int[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this float[] values, int toMin, int toMax)
        {
            int[] result = new int[values.Length];
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this float[] values, int toMin, int toMax, int[] result)
        {
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this float[] values, IRange<float> fromRange, IRange<int> toRange, int[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this float[] values, IRange<float> fromRange, IRange<int> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new int[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this float[] values, IRange<int> toRange, int[] result)
        {
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static float Scale(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (float)value;
            return (float)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this float[] values, float fromMin, float fromMax, float toMin, float toMax, float[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                return values;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (float)((toMax - toMin) * (values[i] - fromMin) / (float)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float Scale(this float value, IRange<float> fromRange, IRange<float> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this float[] values, float fromMin, float fromMax, float toMin, float toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new float[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this float[] values, float toMin, float toMax)
        {
            float[] result = new float[values.Length];
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this float[] values, float toMin, float toMax, float[] result)
        {
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this float[] values, IRange<float> fromRange, IRange<float> toRange, float[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this float[] values, IRange<float> fromRange, IRange<float> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new float[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this float[] values, IRange<float> toRange, float[] result)
        {
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static double Scale(this float value, float fromMin, float fromMax, double toMin, double toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (double)value;
            return (double)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this float[] values, float fromMin, float fromMax, double toMin, double toMax, double[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (double)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (double)((toMax - toMin) * (values[i] - fromMin) / (double)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double Scale(this float value, IRange<float> fromRange, IRange<double> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this float[] values, float fromMin, float fromMax, double toMin, double toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new double[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this float[] values, double toMin, double toMax)
        {
            double[] result = new double[values.Length];
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this float[] values, double toMin, double toMax, double[] result)
        {
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this float[] values, IRange<float> fromRange, IRange<double> toRange, double[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this float[] values, IRange<float> fromRange, IRange<double> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new double[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this float[] values, IRange<double> toRange, double[] result)
        {
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static short Scale(this float value, float fromMin, float fromMax, short toMin, short toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (short)value;
            return (short)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this float[] values, float fromMin, float fromMax, short toMin, short toMax, short[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (short)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (short)((toMax - toMin) * (values[i] - fromMin) / (short)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short Scale(this float value, IRange<float> fromRange, IRange<short> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this float[] values, float fromMin, float fromMax, short toMin, short toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new short[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this float[] values, short toMin, short toMax)
        {
            short[] result = new short[values.Length];
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this float[] values, short toMin, short toMax, short[] result)
        {
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this float[] values, IRange<float> fromRange, IRange<short> toRange, short[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this float[] values, IRange<float> fromRange, IRange<short> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new short[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this float[] values, IRange<short> toRange, short[] result)
        {
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static byte Scale(this float value, float fromMin, float fromMax, byte toMin, byte toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (byte)value;
            return (byte)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this float[] values, float fromMin, float fromMax, byte toMin, byte toMax, byte[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (byte)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (byte)((toMax - toMin) * (values[i] - fromMin) / (byte)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte Scale(this float value, IRange<float> fromRange, IRange<byte> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this float[] values, float fromMin, float fromMax, byte toMin, byte toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new byte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this float[] values, byte toMin, byte toMax)
        {
            byte[] result = new byte[values.Length];
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this float[] values, byte toMin, byte toMax, byte[] result)
        {
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this float[] values, IRange<float> fromRange, IRange<byte> toRange, byte[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this float[] values, IRange<float> fromRange, IRange<byte> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new byte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this float[] values, IRange<byte> toRange, byte[] result)
        {
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte Scale(this float value, float fromMin, float fromMax, sbyte toMin, sbyte toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (sbyte)value;
            return (sbyte)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this float[] values, float fromMin, float fromMax, sbyte toMin, sbyte toMax, sbyte[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (sbyte)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (sbyte)((toMax - toMin) * (values[i] - fromMin) / (sbyte)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte Scale(this float value, IRange<float> fromRange, IRange<sbyte> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this float[] values, float fromMin, float fromMax, sbyte toMin, sbyte toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new sbyte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this float[] values, sbyte toMin, sbyte toMax)
        {
            sbyte[] result = new sbyte[values.Length];
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this float[] values, sbyte toMin, sbyte toMax, sbyte[] result)
        {
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this float[] values, IRange<float> fromRange, IRange<sbyte> toRange, sbyte[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this float[] values, IRange<float> fromRange, IRange<sbyte> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new sbyte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this float[] values, IRange<sbyte> toRange, sbyte[] result)
        {
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static long Scale(this float value, float fromMin, float fromMax, long toMin, long toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (long)value;
            return (long)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this float[] values, float fromMin, float fromMax, long toMin, long toMax, long[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (long)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (long)((toMax - toMin) * (values[i] - fromMin) / (long)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long Scale(this float value, IRange<float> fromRange, IRange<long> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this float[] values, float fromMin, float fromMax, long toMin, long toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new long[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this float[] values, long toMin, long toMax)
        {
            long[] result = new long[values.Length];
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this float[] values, long toMin, long toMax, long[] result)
        {
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this float[] values, IRange<float> fromRange, IRange<long> toRange, long[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this float[] values, IRange<float> fromRange, IRange<long> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new long[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this float[] values, IRange<long> toRange, long[] result)
        {
            float fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static int Scale(this double value, double fromMin, double fromMax, int toMin, int toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (int)value;
            return (int)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this double[] values, double fromMin, double fromMax, int toMin, int toMax, int[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (int)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (int)((toMax - toMin) * (values[i] - fromMin) / (int)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int Scale(this double value, IRange<double> fromRange, IRange<int> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this double[] values, double fromMin, double fromMax, int toMin, int toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new int[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this double[] values, int toMin, int toMax)
        {
            int[] result = new int[values.Length];
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this double[] values, int toMin, int toMax, int[] result)
        {
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this double[] values, IRange<double> fromRange, IRange<int> toRange, int[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this double[] values, IRange<double> fromRange, IRange<int> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new int[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this double[] values, IRange<int> toRange, int[] result)
        {
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static float Scale(this double value, double fromMin, double fromMax, float toMin, float toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (float)value;
            return (float)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this double[] values, double fromMin, double fromMax, float toMin, float toMax, float[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (float)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (float)((toMax - toMin) * (values[i] - fromMin) / (float)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float Scale(this double value, IRange<double> fromRange, IRange<float> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this double[] values, double fromMin, double fromMax, float toMin, float toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new float[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this double[] values, float toMin, float toMax)
        {
            float[] result = new float[values.Length];
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this double[] values, float toMin, float toMax, float[] result)
        {
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this double[] values, IRange<double> fromRange, IRange<float> toRange, float[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this double[] values, IRange<double> fromRange, IRange<float> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new float[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this double[] values, IRange<float> toRange, float[] result)
        {
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static double Scale(this double value, double fromMin, double fromMax, double toMin, double toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (double)value;
            return (double)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this double[] values, double fromMin, double fromMax, double toMin, double toMax, double[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                return values;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (double)((toMax - toMin) * (values[i] - fromMin) / (double)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double Scale(this double value, IRange<double> fromRange, IRange<double> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this double[] values, double fromMin, double fromMax, double toMin, double toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new double[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this double[] values, double toMin, double toMax)
        {
            double[] result = new double[values.Length];
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this double[] values, double toMin, double toMax, double[] result)
        {
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this double[] values, IRange<double> fromRange, IRange<double> toRange, double[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this double[] values, IRange<double> fromRange, IRange<double> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new double[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this double[] values, IRange<double> toRange, double[] result)
        {
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static short Scale(this double value, double fromMin, double fromMax, short toMin, short toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (short)value;
            return (short)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this double[] values, double fromMin, double fromMax, short toMin, short toMax, short[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (short)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (short)((toMax - toMin) * (values[i] - fromMin) / (short)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short Scale(this double value, IRange<double> fromRange, IRange<short> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this double[] values, double fromMin, double fromMax, short toMin, short toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new short[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this double[] values, short toMin, short toMax)
        {
            short[] result = new short[values.Length];
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this double[] values, short toMin, short toMax, short[] result)
        {
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this double[] values, IRange<double> fromRange, IRange<short> toRange, short[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this double[] values, IRange<double> fromRange, IRange<short> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new short[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this double[] values, IRange<short> toRange, short[] result)
        {
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static byte Scale(this double value, double fromMin, double fromMax, byte toMin, byte toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (byte)value;
            return (byte)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this double[] values, double fromMin, double fromMax, byte toMin, byte toMax, byte[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (byte)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (byte)((toMax - toMin) * (values[i] - fromMin) / (byte)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte Scale(this double value, IRange<double> fromRange, IRange<byte> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this double[] values, double fromMin, double fromMax, byte toMin, byte toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new byte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this double[] values, byte toMin, byte toMax)
        {
            byte[] result = new byte[values.Length];
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this double[] values, byte toMin, byte toMax, byte[] result)
        {
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this double[] values, IRange<double> fromRange, IRange<byte> toRange, byte[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this double[] values, IRange<double> fromRange, IRange<byte> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new byte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this double[] values, IRange<byte> toRange, byte[] result)
        {
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte Scale(this double value, double fromMin, double fromMax, sbyte toMin, sbyte toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (sbyte)value;
            return (sbyte)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this double[] values, double fromMin, double fromMax, sbyte toMin, sbyte toMax, sbyte[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (sbyte)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (sbyte)((toMax - toMin) * (values[i] - fromMin) / (sbyte)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte Scale(this double value, IRange<double> fromRange, IRange<sbyte> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this double[] values, double fromMin, double fromMax, sbyte toMin, sbyte toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new sbyte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this double[] values, sbyte toMin, sbyte toMax)
        {
            sbyte[] result = new sbyte[values.Length];
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this double[] values, sbyte toMin, sbyte toMax, sbyte[] result)
        {
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this double[] values, IRange<double> fromRange, IRange<sbyte> toRange, sbyte[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this double[] values, IRange<double> fromRange, IRange<sbyte> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new sbyte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this double[] values, IRange<sbyte> toRange, sbyte[] result)
        {
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static long Scale(this double value, double fromMin, double fromMax, long toMin, long toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (long)value;
            return (long)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this double[] values, double fromMin, double fromMax, long toMin, long toMax, long[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (long)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (long)((toMax - toMin) * (values[i] - fromMin) / (long)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long Scale(this double value, IRange<double> fromRange, IRange<long> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this double[] values, double fromMin, double fromMax, long toMin, long toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new long[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this double[] values, long toMin, long toMax)
        {
            long[] result = new long[values.Length];
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this double[] values, long toMin, long toMax, long[] result)
        {
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this double[] values, IRange<double> fromRange, IRange<long> toRange, long[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this double[] values, IRange<double> fromRange, IRange<long> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new long[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this double[] values, IRange<long> toRange, long[] result)
        {
            double fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static int Scale(this short value, short fromMin, short fromMax, int toMin, int toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (int)value;
            return (int)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this short[] values, short fromMin, short fromMax, int toMin, int toMax, int[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (int)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (int)((toMax - toMin) * (values[i] - fromMin) / (int)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int Scale(this short value, IRange<short> fromRange, IRange<int> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this short[] values, short fromMin, short fromMax, int toMin, int toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new int[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this short[] values, int toMin, int toMax)
        {
            int[] result = new int[values.Length];
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this short[] values, int toMin, int toMax, int[] result)
        {
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this short[] values, IRange<short> fromRange, IRange<int> toRange, int[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this short[] values, IRange<short> fromRange, IRange<int> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new int[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this short[] values, IRange<int> toRange, int[] result)
        {
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static float Scale(this short value, short fromMin, short fromMax, float toMin, float toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (float)value;
            return (float)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this short[] values, short fromMin, short fromMax, float toMin, float toMax, float[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (float)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (float)((toMax - toMin) * (values[i] - fromMin) / (float)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float Scale(this short value, IRange<short> fromRange, IRange<float> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this short[] values, short fromMin, short fromMax, float toMin, float toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new float[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this short[] values, float toMin, float toMax)
        {
            float[] result = new float[values.Length];
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this short[] values, float toMin, float toMax, float[] result)
        {
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this short[] values, IRange<short> fromRange, IRange<float> toRange, float[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this short[] values, IRange<short> fromRange, IRange<float> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new float[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this short[] values, IRange<float> toRange, float[] result)
        {
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static double Scale(this short value, short fromMin, short fromMax, double toMin, double toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (double)value;
            return (double)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this short[] values, short fromMin, short fromMax, double toMin, double toMax, double[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (double)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (double)((toMax - toMin) * (values[i] - fromMin) / (double)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double Scale(this short value, IRange<short> fromRange, IRange<double> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this short[] values, short fromMin, short fromMax, double toMin, double toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new double[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this short[] values, double toMin, double toMax)
        {
            double[] result = new double[values.Length];
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this short[] values, double toMin, double toMax, double[] result)
        {
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this short[] values, IRange<short> fromRange, IRange<double> toRange, double[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this short[] values, IRange<short> fromRange, IRange<double> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new double[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this short[] values, IRange<double> toRange, double[] result)
        {
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static short Scale(this short value, short fromMin, short fromMax, short toMin, short toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (short)value;
            return (short)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this short[] values, short fromMin, short fromMax, short toMin, short toMax, short[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                return values;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (short)((toMax - toMin) * (values[i] - fromMin) / (short)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short Scale(this short value, IRange<short> fromRange, IRange<short> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this short[] values, short fromMin, short fromMax, short toMin, short toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new short[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this short[] values, short toMin, short toMax)
        {
            short[] result = new short[values.Length];
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this short[] values, short toMin, short toMax, short[] result)
        {
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this short[] values, IRange<short> fromRange, IRange<short> toRange, short[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this short[] values, IRange<short> fromRange, IRange<short> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new short[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this short[] values, IRange<short> toRange, short[] result)
        {
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static byte Scale(this short value, short fromMin, short fromMax, byte toMin, byte toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (byte)value;
            return (byte)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this short[] values, short fromMin, short fromMax, byte toMin, byte toMax, byte[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (byte)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (byte)((toMax - toMin) * (values[i] - fromMin) / (byte)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte Scale(this short value, IRange<short> fromRange, IRange<byte> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this short[] values, short fromMin, short fromMax, byte toMin, byte toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new byte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this short[] values, byte toMin, byte toMax)
        {
            byte[] result = new byte[values.Length];
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this short[] values, byte toMin, byte toMax, byte[] result)
        {
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this short[] values, IRange<short> fromRange, IRange<byte> toRange, byte[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this short[] values, IRange<short> fromRange, IRange<byte> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new byte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this short[] values, IRange<byte> toRange, byte[] result)
        {
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte Scale(this short value, short fromMin, short fromMax, sbyte toMin, sbyte toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (sbyte)value;
            return (sbyte)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this short[] values, short fromMin, short fromMax, sbyte toMin, sbyte toMax, sbyte[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (sbyte)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (sbyte)((toMax - toMin) * (values[i] - fromMin) / (sbyte)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte Scale(this short value, IRange<short> fromRange, IRange<sbyte> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this short[] values, short fromMin, short fromMax, sbyte toMin, sbyte toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new sbyte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this short[] values, sbyte toMin, sbyte toMax)
        {
            sbyte[] result = new sbyte[values.Length];
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this short[] values, sbyte toMin, sbyte toMax, sbyte[] result)
        {
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this short[] values, IRange<short> fromRange, IRange<sbyte> toRange, sbyte[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this short[] values, IRange<short> fromRange, IRange<sbyte> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new sbyte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this short[] values, IRange<sbyte> toRange, sbyte[] result)
        {
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static long Scale(this short value, short fromMin, short fromMax, long toMin, long toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (long)value;
            return (long)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this short[] values, short fromMin, short fromMax, long toMin, long toMax, long[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (long)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (long)((toMax - toMin) * (values[i] - fromMin) / (long)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long Scale(this short value, IRange<short> fromRange, IRange<long> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this short[] values, short fromMin, short fromMax, long toMin, long toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new long[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this short[] values, long toMin, long toMax)
        {
            long[] result = new long[values.Length];
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this short[] values, long toMin, long toMax, long[] result)
        {
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this short[] values, IRange<short> fromRange, IRange<long> toRange, long[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this short[] values, IRange<short> fromRange, IRange<long> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new long[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this short[] values, IRange<long> toRange, long[] result)
        {
            short fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static int Scale(this byte value, byte fromMin, byte fromMax, int toMin, int toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (int)value;
            return (int)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this byte[] values, byte fromMin, byte fromMax, int toMin, int toMax, int[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (int)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (int)((toMax - toMin) * (values[i] - fromMin) / (int)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int Scale(this byte value, IRange<byte> fromRange, IRange<int> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this byte[] values, byte fromMin, byte fromMax, int toMin, int toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new int[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this byte[] values, int toMin, int toMax)
        {
            int[] result = new int[values.Length];
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this byte[] values, int toMin, int toMax, int[] result)
        {
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this byte[] values, IRange<byte> fromRange, IRange<int> toRange, int[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this byte[] values, IRange<byte> fromRange, IRange<int> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new int[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this byte[] values, IRange<int> toRange, int[] result)
        {
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static float Scale(this byte value, byte fromMin, byte fromMax, float toMin, float toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (float)value;
            return (float)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this byte[] values, byte fromMin, byte fromMax, float toMin, float toMax, float[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (float)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (float)((toMax - toMin) * (values[i] - fromMin) / (float)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float Scale(this byte value, IRange<byte> fromRange, IRange<float> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this byte[] values, byte fromMin, byte fromMax, float toMin, float toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new float[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this byte[] values, float toMin, float toMax)
        {
            float[] result = new float[values.Length];
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this byte[] values, float toMin, float toMax, float[] result)
        {
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this byte[] values, IRange<byte> fromRange, IRange<float> toRange, float[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this byte[] values, IRange<byte> fromRange, IRange<float> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new float[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this byte[] values, IRange<float> toRange, float[] result)
        {
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static double Scale(this byte value, byte fromMin, byte fromMax, double toMin, double toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (double)value;
            return (double)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this byte[] values, byte fromMin, byte fromMax, double toMin, double toMax, double[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (double)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (double)((toMax - toMin) * (values[i] - fromMin) / (double)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double Scale(this byte value, IRange<byte> fromRange, IRange<double> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this byte[] values, byte fromMin, byte fromMax, double toMin, double toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new double[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this byte[] values, double toMin, double toMax)
        {
            double[] result = new double[values.Length];
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this byte[] values, double toMin, double toMax, double[] result)
        {
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this byte[] values, IRange<byte> fromRange, IRange<double> toRange, double[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this byte[] values, IRange<byte> fromRange, IRange<double> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new double[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this byte[] values, IRange<double> toRange, double[] result)
        {
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static short Scale(this byte value, byte fromMin, byte fromMax, short toMin, short toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (short)value;
            return (short)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this byte[] values, byte fromMin, byte fromMax, short toMin, short toMax, short[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (short)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (short)((toMax - toMin) * (values[i] - fromMin) / (short)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short Scale(this byte value, IRange<byte> fromRange, IRange<short> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this byte[] values, byte fromMin, byte fromMax, short toMin, short toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new short[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this byte[] values, short toMin, short toMax)
        {
            short[] result = new short[values.Length];
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this byte[] values, short toMin, short toMax, short[] result)
        {
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this byte[] values, IRange<byte> fromRange, IRange<short> toRange, short[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this byte[] values, IRange<byte> fromRange, IRange<short> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new short[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this byte[] values, IRange<short> toRange, short[] result)
        {
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static byte Scale(this byte value, byte fromMin, byte fromMax, byte toMin, byte toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (byte)value;
            return (byte)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this byte[] values, byte fromMin, byte fromMax, byte toMin, byte toMax, byte[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                return values;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (byte)((toMax - toMin) * (values[i] - fromMin) / (byte)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte Scale(this byte value, IRange<byte> fromRange, IRange<byte> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this byte[] values, byte fromMin, byte fromMax, byte toMin, byte toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new byte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this byte[] values, byte toMin, byte toMax)
        {
            byte[] result = new byte[values.Length];
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this byte[] values, byte toMin, byte toMax, byte[] result)
        {
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this byte[] values, IRange<byte> fromRange, IRange<byte> toRange, byte[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this byte[] values, IRange<byte> fromRange, IRange<byte> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new byte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this byte[] values, IRange<byte> toRange, byte[] result)
        {
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte Scale(this byte value, byte fromMin, byte fromMax, sbyte toMin, sbyte toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (sbyte)value;
            return (sbyte)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this byte[] values, byte fromMin, byte fromMax, sbyte toMin, sbyte toMax, sbyte[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (sbyte)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (sbyte)((toMax - toMin) * (values[i] - fromMin) / (sbyte)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte Scale(this byte value, IRange<byte> fromRange, IRange<sbyte> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this byte[] values, byte fromMin, byte fromMax, sbyte toMin, sbyte toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new sbyte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this byte[] values, sbyte toMin, sbyte toMax)
        {
            sbyte[] result = new sbyte[values.Length];
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this byte[] values, sbyte toMin, sbyte toMax, sbyte[] result)
        {
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this byte[] values, IRange<byte> fromRange, IRange<sbyte> toRange, sbyte[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this byte[] values, IRange<byte> fromRange, IRange<sbyte> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new sbyte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this byte[] values, IRange<sbyte> toRange, sbyte[] result)
        {
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static long Scale(this byte value, byte fromMin, byte fromMax, long toMin, long toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (long)value;
            return (long)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this byte[] values, byte fromMin, byte fromMax, long toMin, long toMax, long[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (long)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (long)((toMax - toMin) * (values[i] - fromMin) / (long)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long Scale(this byte value, IRange<byte> fromRange, IRange<long> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this byte[] values, byte fromMin, byte fromMax, long toMin, long toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new long[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this byte[] values, long toMin, long toMax)
        {
            long[] result = new long[values.Length];
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this byte[] values, long toMin, long toMax, long[] result)
        {
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this byte[] values, IRange<byte> fromRange, IRange<long> toRange, long[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this byte[] values, IRange<byte> fromRange, IRange<long> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new long[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this byte[] values, IRange<long> toRange, long[] result)
        {
            byte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static int Scale(this sbyte value, sbyte fromMin, sbyte fromMax, int toMin, int toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (int)value;
            return (int)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this sbyte[] values, sbyte fromMin, sbyte fromMax, int toMin, int toMax, int[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (int)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (int)((toMax - toMin) * (values[i] - fromMin) / (int)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int Scale(this sbyte value, IRange<sbyte> fromRange, IRange<int> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this sbyte[] values, sbyte fromMin, sbyte fromMax, int toMin, int toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new int[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this sbyte[] values, int toMin, int toMax)
        {
            int[] result = new int[values.Length];
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this sbyte[] values, int toMin, int toMax, int[] result)
        {
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this sbyte[] values, IRange<sbyte> fromRange, IRange<int> toRange, int[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this sbyte[] values, IRange<sbyte> fromRange, IRange<int> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new int[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this sbyte[] values, IRange<int> toRange, int[] result)
        {
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static float Scale(this sbyte value, sbyte fromMin, sbyte fromMax, float toMin, float toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (float)value;
            return (float)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this sbyte[] values, sbyte fromMin, sbyte fromMax, float toMin, float toMax, float[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (float)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (float)((toMax - toMin) * (values[i] - fromMin) / (float)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float Scale(this sbyte value, IRange<sbyte> fromRange, IRange<float> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this sbyte[] values, sbyte fromMin, sbyte fromMax, float toMin, float toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new float[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this sbyte[] values, float toMin, float toMax)
        {
            float[] result = new float[values.Length];
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this sbyte[] values, float toMin, float toMax, float[] result)
        {
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this sbyte[] values, IRange<sbyte> fromRange, IRange<float> toRange, float[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this sbyte[] values, IRange<sbyte> fromRange, IRange<float> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new float[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this sbyte[] values, IRange<float> toRange, float[] result)
        {
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static double Scale(this sbyte value, sbyte fromMin, sbyte fromMax, double toMin, double toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (double)value;
            return (double)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this sbyte[] values, sbyte fromMin, sbyte fromMax, double toMin, double toMax, double[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (double)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (double)((toMax - toMin) * (values[i] - fromMin) / (double)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double Scale(this sbyte value, IRange<sbyte> fromRange, IRange<double> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this sbyte[] values, sbyte fromMin, sbyte fromMax, double toMin, double toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new double[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this sbyte[] values, double toMin, double toMax)
        {
            double[] result = new double[values.Length];
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this sbyte[] values, double toMin, double toMax, double[] result)
        {
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this sbyte[] values, IRange<sbyte> fromRange, IRange<double> toRange, double[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this sbyte[] values, IRange<sbyte> fromRange, IRange<double> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new double[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this sbyte[] values, IRange<double> toRange, double[] result)
        {
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static short Scale(this sbyte value, sbyte fromMin, sbyte fromMax, short toMin, short toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (short)value;
            return (short)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this sbyte[] values, sbyte fromMin, sbyte fromMax, short toMin, short toMax, short[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (short)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (short)((toMax - toMin) * (values[i] - fromMin) / (short)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short Scale(this sbyte value, IRange<sbyte> fromRange, IRange<short> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this sbyte[] values, sbyte fromMin, sbyte fromMax, short toMin, short toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new short[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this sbyte[] values, short toMin, short toMax)
        {
            short[] result = new short[values.Length];
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this sbyte[] values, short toMin, short toMax, short[] result)
        {
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this sbyte[] values, IRange<sbyte> fromRange, IRange<short> toRange, short[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this sbyte[] values, IRange<sbyte> fromRange, IRange<short> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new short[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this sbyte[] values, IRange<short> toRange, short[] result)
        {
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static byte Scale(this sbyte value, sbyte fromMin, sbyte fromMax, byte toMin, byte toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (byte)value;
            return (byte)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this sbyte[] values, sbyte fromMin, sbyte fromMax, byte toMin, byte toMax, byte[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (byte)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (byte)((toMax - toMin) * (values[i] - fromMin) / (byte)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte Scale(this sbyte value, IRange<sbyte> fromRange, IRange<byte> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this sbyte[] values, sbyte fromMin, sbyte fromMax, byte toMin, byte toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new byte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this sbyte[] values, byte toMin, byte toMax)
        {
            byte[] result = new byte[values.Length];
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this sbyte[] values, byte toMin, byte toMax, byte[] result)
        {
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this sbyte[] values, IRange<sbyte> fromRange, IRange<byte> toRange, byte[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this sbyte[] values, IRange<sbyte> fromRange, IRange<byte> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new byte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this sbyte[] values, IRange<byte> toRange, byte[] result)
        {
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte Scale(this sbyte value, sbyte fromMin, sbyte fromMax, sbyte toMin, sbyte toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (sbyte)value;
            return (sbyte)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this sbyte[] values, sbyte fromMin, sbyte fromMax, sbyte toMin, sbyte toMax, sbyte[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                return values;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (sbyte)((toMax - toMin) * (values[i] - fromMin) / (sbyte)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte Scale(this sbyte value, IRange<sbyte> fromRange, IRange<sbyte> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this sbyte[] values, sbyte fromMin, sbyte fromMax, sbyte toMin, sbyte toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new sbyte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this sbyte[] values, sbyte toMin, sbyte toMax)
        {
            sbyte[] result = new sbyte[values.Length];
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this sbyte[] values, sbyte toMin, sbyte toMax, sbyte[] result)
        {
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this sbyte[] values, IRange<sbyte> fromRange, IRange<sbyte> toRange, sbyte[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this sbyte[] values, IRange<sbyte> fromRange, IRange<sbyte> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new sbyte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this sbyte[] values, IRange<sbyte> toRange, sbyte[] result)
        {
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static long Scale(this sbyte value, sbyte fromMin, sbyte fromMax, long toMin, long toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (long)value;
            return (long)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this sbyte[] values, sbyte fromMin, sbyte fromMax, long toMin, long toMax, long[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (long)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (long)((toMax - toMin) * (values[i] - fromMin) / (long)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long Scale(this sbyte value, IRange<sbyte> fromRange, IRange<long> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this sbyte[] values, sbyte fromMin, sbyte fromMax, long toMin, long toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new long[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this sbyte[] values, long toMin, long toMax)
        {
            long[] result = new long[values.Length];
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this sbyte[] values, long toMin, long toMax, long[] result)
        {
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this sbyte[] values, IRange<sbyte> fromRange, IRange<long> toRange, long[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this sbyte[] values, IRange<sbyte> fromRange, IRange<long> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new long[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this sbyte[] values, IRange<long> toRange, long[] result)
        {
            sbyte fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static int Scale(this long value, long fromMin, long fromMax, int toMin, int toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (int)value;
            return (int)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this long[] values, long fromMin, long fromMax, int toMin, int toMax, int[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (int)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (int)((toMax - toMin) * (values[i] - fromMin) / (int)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int Scale(this long value, IRange<long> fromRange, IRange<int> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this long[] values, long fromMin, long fromMax, int toMin, int toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new int[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this long[] values, int toMin, int toMax)
        {
            int[] result = new int[values.Length];
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this long[] values, int toMin, int toMax, int[] result)
        {
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this long[] values, IRange<long> fromRange, IRange<int> toRange, int[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this long[] values, IRange<long> fromRange, IRange<int> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new int[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static int[] Scale(this long[] values, IRange<int> toRange, int[] result)
        {
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static float Scale(this long value, long fromMin, long fromMax, float toMin, float toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (float)value;
            return (float)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this long[] values, long fromMin, long fromMax, float toMin, float toMax, float[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (float)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (float)((toMax - toMin) * (values[i] - fromMin) / (float)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float Scale(this long value, IRange<long> fromRange, IRange<float> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this long[] values, long fromMin, long fromMax, float toMin, float toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new float[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this long[] values, float toMin, float toMax)
        {
            float[] result = new float[values.Length];
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this long[] values, float toMin, float toMax, float[] result)
        {
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this long[] values, IRange<long> fromRange, IRange<float> toRange, float[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this long[] values, IRange<long> fromRange, IRange<float> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new float[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static float[] Scale(this long[] values, IRange<float> toRange, float[] result)
        {
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static double Scale(this long value, long fromMin, long fromMax, double toMin, double toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (double)value;
            return (double)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this long[] values, long fromMin, long fromMax, double toMin, double toMax, double[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (double)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (double)((toMax - toMin) * (values[i] - fromMin) / (double)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double Scale(this long value, IRange<long> fromRange, IRange<double> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this long[] values, long fromMin, long fromMax, double toMin, double toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new double[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this long[] values, double toMin, double toMax)
        {
            double[] result = new double[values.Length];
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this long[] values, double toMin, double toMax, double[] result)
        {
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this long[] values, IRange<long> fromRange, IRange<double> toRange, double[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this long[] values, IRange<long> fromRange, IRange<double> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new double[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static double[] Scale(this long[] values, IRange<double> toRange, double[] result)
        {
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static short Scale(this long value, long fromMin, long fromMax, short toMin, short toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (short)value;
            return (short)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this long[] values, long fromMin, long fromMax, short toMin, short toMax, short[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (short)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (short)((toMax - toMin) * (values[i] - fromMin) / (short)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short Scale(this long value, IRange<long> fromRange, IRange<short> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this long[] values, long fromMin, long fromMax, short toMin, short toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new short[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this long[] values, short toMin, short toMax)
        {
            short[] result = new short[values.Length];
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this long[] values, short toMin, short toMax, short[] result)
        {
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this long[] values, IRange<long> fromRange, IRange<short> toRange, short[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this long[] values, IRange<long> fromRange, IRange<short> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new short[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static short[] Scale(this long[] values, IRange<short> toRange, short[] result)
        {
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static byte Scale(this long value, long fromMin, long fromMax, byte toMin, byte toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (byte)value;
            return (byte)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this long[] values, long fromMin, long fromMax, byte toMin, byte toMax, byte[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (byte)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (byte)((toMax - toMin) * (values[i] - fromMin) / (byte)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte Scale(this long value, IRange<long> fromRange, IRange<byte> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this long[] values, long fromMin, long fromMax, byte toMin, byte toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new byte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this long[] values, byte toMin, byte toMax)
        {
            byte[] result = new byte[values.Length];
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this long[] values, byte toMin, byte toMax, byte[] result)
        {
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this long[] values, IRange<long> fromRange, IRange<byte> toRange, byte[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this long[] values, IRange<long> fromRange, IRange<byte> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new byte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static byte[] Scale(this long[] values, IRange<byte> toRange, byte[] result)
        {
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte Scale(this long value, long fromMin, long fromMax, sbyte toMin, sbyte toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (sbyte)value;
            return (sbyte)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this long[] values, long fromMin, long fromMax, sbyte toMin, sbyte toMax, sbyte[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                for (int i = 0; i < values.Length; i++)
                    result[i] = (sbyte)values[i];
                return result;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (sbyte)((toMax - toMin) * (values[i] - fromMin) / (sbyte)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte Scale(this long value, IRange<long> fromRange, IRange<sbyte> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this long[] values, long fromMin, long fromMax, sbyte toMin, sbyte toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new sbyte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this long[] values, sbyte toMin, sbyte toMax)
        {
            sbyte[] result = new sbyte[values.Length];
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this long[] values, sbyte toMin, sbyte toMax, sbyte[] result)
        {
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this long[] values, IRange<long> fromRange, IRange<sbyte> toRange, sbyte[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this long[] values, IRange<long> fromRange, IRange<sbyte> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new sbyte[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static sbyte[] Scale(this long[] values, IRange<sbyte> toRange, sbyte[] result)
        {
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        /// <summary>
        ///   Converts a value from one scale to another scale.
        /// </summary>
        /// 
        public static long Scale(this long value, long fromMin, long fromMax, long toMin, long toMax)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
                return (long)value;
            return (long)((toMax - toMin) * (value - fromMin) / (fromMax - fromMin) + toMin);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this long[] values, long fromMin, long fromMax, long toMin, long toMax, long[] result)
        {
            if (fromMin == fromMax && fromMin == toMin && fromMin == toMax)
            {
                return values;
            }

            for (int i = 0; i < values.Length; i++)
                result[i] = (long)((toMax - toMin) * (values[i] - fromMin) / (long)(fromMax - fromMin) + toMin);
            return result;
        }



        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long Scale(this long value, IRange<long> fromRange, IRange<long> toRange)
        {
            return Scale(value, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this long[] values, long fromMin, long fromMax, long toMin, long toMax)
        {
            return Scale(values, fromMin, fromMax, toMin, toMax, new long[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this long[] values, long toMin, long toMax)
        {
            long[] result = new long[values.Length];
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this long[] values, long toMin, long toMax, long[] result)
        {
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toMin, toMax, result);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this long[] values, IRange<long> fromRange, IRange<long> toRange, long[] result)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, result);
        }


        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this long[] values, IRange<long> fromRange, IRange<long> toRange)
        {
            return Scale(values, fromRange.Min, fromRange.Max, toRange.Min, toRange.Max, new long[values.Length]);
        }

        /// <summary>
        ///   Converts values from one scale to another scale.
        /// </summary>
        /// 
        public static long[] Scale(this long[] values, IRange<long> toRange, long[] result)
        {
            long fromMin, fromMax;
            values.GetRange(out fromMin, out fromMax);
            return Scale(values, fromMin, fromMax, toRange.Min, toRange.Max, result);
        }
        #endregion

        #region Others

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
        ///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static int[] Range(int n)
        {
            int[] r = new int[(int)n];
            for (int i = 0; i < r.Length; i++)
                r[i] = (int)i;
            return r;
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        ///
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static int[] Range(int a, int b)
        {
            if (a == b)
                return new int[] { };

            int[] r;

            if (b > a)
            {
                r = new int[(int)(b - a)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (int)(a++);
            }
            else
            {
                r = new int[(int)(a - b)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (int)(a--);
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<int> EnumerableRange(int n)
        {
            for (int i = 0; i < n; i++)
                yield return (int)i;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<int> EnumerableRange(int a, int b)
        {
            if (a == b)
                yield break;

            if (b > a)
            {
                int n = (int)(b - a);
                for (int i = 0; i < n; i++)
                    yield return (int)(a++);
            }
            else
            {
                int n = (int)(a - b);
                for (int i = 0; i < n; i++)
                    yield return (int)(a--);
            }
        }


        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static int[] Range(int a, int b, int stepSize)
        {
            if (a == b)
                return new int[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            int[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new int[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (int)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new int[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (int)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new int[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (int)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<int> EnumerableRange(int a, int b, int stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            int last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (int)(a + i * stepSize);
                last = (int)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (int)(a - i * stepSize);
                    last = (int)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (int)(a + i * stepSize);
                    last = (int)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(int a, int b, float stepSize)
        {
            if (a == b)
                return new float[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));

                r = new float[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (float)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<float> EnumerableRange(int a, int b, float stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (float)(a + i * stepSize);
                last = (float)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a - i * stepSize);
                    last = (float)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a + i * stepSize);
                    last = (float)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(int a, int b, double stepSize)
        {
            if (a == b)
                return new double[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new double[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (double)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<double> EnumerableRange(int a, int b, double stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (double)(a + i * stepSize);
                last = (double)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a - i * stepSize);
                    last = (double)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a + i * stepSize);
                    last = (double)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(float n)
        {
            float[] r = new float[(int)n];
            for (int i = 0; i < r.Length; i++)
                r[i] = (float)i;
            return r;
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        ///
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(float a, float b)
        {
            if (a == b)
                return new float[] { };

            float[] r;

            if (b > a)
            {
                r = new float[(int)(b - a)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (float)(a++);
            }
            else
            {
                r = new float[(int)(a - b)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (float)(a--);
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<float> EnumerableRange(float n)
        {
            for (float i = 0; i < n; i++)
                yield return (float)i;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<float> EnumerableRange(float a, float b)
        {
            if (a == b)
                yield break;

            if (b > a)
            {
                int n = (int)(b - a);
                for (int i = 0; i < n; i++)
                    yield return (float)(a++);
            }
            else
            {
                int n = (int)(a - b);
                for (int i = 0; i < n; i++)
                    yield return (float)(a--);
            }
        }


        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(float a, float b, int stepSize)
        {
            if (a == b)
                return new float[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));

                r = new float[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (float)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<float> EnumerableRange(float a, float b, int stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (float)(a + i * stepSize);
                last = (float)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a - i * stepSize);
                    last = (float)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a + i * stepSize);
                    last = (float)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(float a, float b, float stepSize)
        {
            if (a == b)
                return new float[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new float[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (float)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<float> EnumerableRange(float a, float b, float stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (float)(a + i * stepSize);
                last = (float)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a - i * stepSize);
                    last = (float)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a + i * stepSize);
                    last = (float)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(float a, float b, short stepSize)
        {
            if (a == b)
                return new float[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));

                r = new float[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (float)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<float> EnumerableRange(float a, float b, short stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (float)(a + i * stepSize);
                last = (float)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a - i * stepSize);
                    last = (float)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a + i * stepSize);
                    last = (float)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(float a, float b, byte stepSize)
        {
            if (a == b)
                return new float[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));

                r = new float[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (float)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<float> EnumerableRange(float a, float b, byte stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (float)(a + i * stepSize);
                last = (float)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a - i * stepSize);
                    last = (float)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a + i * stepSize);
                    last = (float)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(float a, float b, sbyte stepSize)
        {
            if (a == b)
                return new float[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));

                r = new float[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (float)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<float> EnumerableRange(float a, float b, sbyte stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (float)(a + i * stepSize);
                last = (float)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a - i * stepSize);
                    last = (float)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a + i * stepSize);
                    last = (float)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(float a, float b, long stepSize)
        {
            if (a == b)
                return new float[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));

                r = new float[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (float)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<float> EnumerableRange(float a, float b, long stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (float)(a + i * stepSize);
                last = (float)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a - i * stepSize);
                    last = (float)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a + i * stepSize);
                    last = (float)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(float a, float b, ulong stepSize)
        {
            if (a == b)
                return new float[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));

                r = new float[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (float)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<float> EnumerableRange(float a, float b, ulong stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (float)(a + i * stepSize);
                last = (float)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a - i * stepSize);
                    last = (float)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a + i * stepSize);
                    last = (float)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(float a, float b, ushort stepSize)
        {
            if (a == b)
                return new float[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));

                r = new float[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (float)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<float> EnumerableRange(float a, float b, ushort stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (float)(a + i * stepSize);
                last = (float)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a - i * stepSize);
                    last = (float)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a + i * stepSize);
                    last = (float)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(double n)
        {
            double[] r = new double[(int)n];
            for (int i = 0; i < r.Length; i++)
                r[i] = (double)i;
            return r;
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        ///
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(double a, double b)
        {
            if (a == b)
                return new double[] { };

            double[] r;

            if (b > a)
            {
                r = new double[(int)(b - a)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (double)(a++);
            }
            else
            {
                r = new double[(int)(a - b)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (double)(a--);
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<double> EnumerableRange(double n)
        {
            for (double i = 0; i < n; i++)
                yield return (double)i;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<double> EnumerableRange(double a, double b)
        {
            if (a == b)
                yield break;

            if (b > a)
            {
                int n = (int)(b - a);
                for (int i = 0; i < n; i++)
                    yield return (double)(a++);
            }
            else
            {
                int n = (int)(a - b);
                for (int i = 0; i < n; i++)
                    yield return (double)(a--);
            }
        }


        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(double a, double b, int stepSize)
        {
            if (a == b)
                return new double[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new double[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (double)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<double> EnumerableRange(double a, double b, int stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (double)(a + i * stepSize);
                last = (double)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a - i * stepSize);
                    last = (double)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a + i * stepSize);
                    last = (double)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(double a, double b, double stepSize)
        {
            if (a == b)
                return new double[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new double[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (double)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<double> EnumerableRange(double a, double b, double stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (double)(a + i * stepSize);
                last = (double)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a - i * stepSize);
                    last = (double)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a + i * stepSize);
                    last = (double)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(double a, double b, short stepSize)
        {
            if (a == b)
                return new double[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new double[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (double)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<double> EnumerableRange(double a, double b, short stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (double)(a + i * stepSize);
                last = (double)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a - i * stepSize);
                    last = (double)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a + i * stepSize);
                    last = (double)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(double a, double b, byte stepSize)
        {
            if (a == b)
                return new double[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new double[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (double)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<double> EnumerableRange(double a, double b, byte stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (double)(a + i * stepSize);
                last = (double)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a - i * stepSize);
                    last = (double)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a + i * stepSize);
                    last = (double)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(double a, double b, sbyte stepSize)
        {
            if (a == b)
                return new double[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new double[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (double)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<double> EnumerableRange(double a, double b, sbyte stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (double)(a + i * stepSize);
                last = (double)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a - i * stepSize);
                    last = (double)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a + i * stepSize);
                    last = (double)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(double a, double b, long stepSize)
        {
            if (a == b)
                return new double[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new double[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (double)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<double> EnumerableRange(double a, double b, long stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (double)(a + i * stepSize);
                last = (double)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a - i * stepSize);
                    last = (double)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a + i * stepSize);
                    last = (double)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(double a, double b, ulong stepSize)
        {
            if (a == b)
                return new double[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new double[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (double)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<double> EnumerableRange(double a, double b, ulong stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (double)(a + i * stepSize);
                last = (double)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a - i * stepSize);
                    last = (double)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a + i * stepSize);
                    last = (double)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(double a, double b, ushort stepSize)
        {
            if (a == b)
                return new double[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new double[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (double)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<double> EnumerableRange(double a, double b, ushort stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (double)(a + i * stepSize);
                last = (double)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a - i * stepSize);
                    last = (double)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a + i * stepSize);
                    last = (double)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static short[] Range(short n)
        {
            short[] r = new short[(int)n];
            for (int i = 0; i < r.Length; i++)
                r[i] = (short)i;
            return r;
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        ///
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static short[] Range(short a, short b)
        {
            if (a == b)
                return new short[] { };

            short[] r;

            if (b > a)
            {
                r = new short[(int)(b - a)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (short)(a++);
            }
            else
            {
                r = new short[(int)(a - b)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (short)(a--);
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<short> EnumerableRange(short n)
        {
            for (short i = 0; i < n; i++)
                yield return (short)i;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<short> EnumerableRange(short a, short b)
        {
            if (a == b)
                yield break;

            if (b > a)
            {
                int n = (int)(b - a);
                for (int i = 0; i < n; i++)
                    yield return (short)(a++);
            }
            else
            {
                int n = (int)(a - b);
                for (int i = 0; i < n; i++)
                    yield return (short)(a--);
            }
        }


        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(short a, short b, float stepSize)
        {
            if (a == b)
                return new float[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));

                r = new float[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (float)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<float> EnumerableRange(short a, short b, float stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (float)(a + i * stepSize);
                last = (float)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a - i * stepSize);
                    last = (float)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a + i * stepSize);
                    last = (float)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(short a, short b, double stepSize)
        {
            if (a == b)
                return new double[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new double[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (double)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<double> EnumerableRange(short a, short b, double stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (double)(a + i * stepSize);
                last = (double)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a - i * stepSize);
                    last = (double)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a + i * stepSize);
                    last = (double)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static short[] Range(short a, short b, short stepSize)
        {
            if (a == b)
                return new short[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            short[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new short[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (short)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new short[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (short)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new short[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (short)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<short> EnumerableRange(short a, short b, short stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            short last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (short)(a + i * stepSize);
                last = (short)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (short)(a - i * stepSize);
                    last = (short)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (short)(a + i * stepSize);
                    last = (short)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static byte[] Range(byte n)
        {
            byte[] r = new byte[(int)n];
            for (int i = 0; i < r.Length; i++)
                r[i] = (byte)i;
            return r;
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        ///
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static byte[] Range(byte a, byte b)
        {
            if (a == b)
                return new byte[] { };

            byte[] r;

            if (b > a)
            {
                r = new byte[(int)(b - a)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (byte)(a++);
            }
            else
            {
                r = new byte[(int)(a - b)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (byte)(a--);
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<byte> EnumerableRange(byte n)
        {
            for (byte i = 0; i < n; i++)
                yield return (byte)i;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<byte> EnumerableRange(byte a, byte b)
        {
            if (a == b)
                yield break;

            if (b > a)
            {
                int n = (int)(b - a);
                for (int i = 0; i < n; i++)
                    yield return (byte)(a++);
            }
            else
            {
                int n = (int)(a - b);
                for (int i = 0; i < n; i++)
                    yield return (byte)(a--);
            }
        }


        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(byte a, byte b, float stepSize)
        {
            if (a == b)
                return new float[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));

                r = new float[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (float)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<float> EnumerableRange(byte a, byte b, float stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (float)(a + i * stepSize);
                last = (float)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a - i * stepSize);
                    last = (float)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a + i * stepSize);
                    last = (float)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(byte a, byte b, double stepSize)
        {
            if (a == b)
                return new double[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new double[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (double)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<double> EnumerableRange(byte a, byte b, double stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (double)(a + i * stepSize);
                last = (double)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a - i * stepSize);
                    last = (double)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a + i * stepSize);
                    last = (double)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static byte[] Range(byte a, byte b, byte stepSize)
        {
            if (a == b)
                return new byte[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            byte[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new byte[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (byte)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new byte[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (byte)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new byte[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (byte)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<byte> EnumerableRange(byte a, byte b, byte stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            byte last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (byte)(a + i * stepSize);
                last = (byte)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (byte)(a - i * stepSize);
                    last = (byte)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (byte)(a + i * stepSize);
                    last = (byte)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static sbyte[] Range(sbyte n)
        {
            sbyte[] r = new sbyte[(int)n];
            for (int i = 0; i < r.Length; i++)
                r[i] = (sbyte)i;
            return r;
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        ///
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static sbyte[] Range(sbyte a, sbyte b)
        {
            if (a == b)
                return new sbyte[] { };

            sbyte[] r;

            if (b > a)
            {
                r = new sbyte[(int)(b - a)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (sbyte)(a++);
            }
            else
            {
                r = new sbyte[(int)(a - b)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (sbyte)(a--);
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<sbyte> EnumerableRange(sbyte n)
        {
            for (sbyte i = 0; i < n; i++)
                yield return (sbyte)i;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<sbyte> EnumerableRange(sbyte a, sbyte b)
        {
            if (a == b)
                yield break;

            if (b > a)
            {
                int n = (int)(b - a);
                for (int i = 0; i < n; i++)
                    yield return (sbyte)(a++);
            }
            else
            {
                int n = (int)(a - b);
                for (int i = 0; i < n; i++)
                    yield return (sbyte)(a--);
            }
        }


        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(sbyte a, sbyte b, float stepSize)
        {
            if (a == b)
                return new float[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));

                r = new float[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (float)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<float> EnumerableRange(sbyte a, sbyte b, float stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (float)(a + i * stepSize);
                last = (float)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a - i * stepSize);
                    last = (float)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a + i * stepSize);
                    last = (float)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(sbyte a, sbyte b, double stepSize)
        {
            if (a == b)
                return new double[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new double[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (double)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<double> EnumerableRange(sbyte a, sbyte b, double stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (double)(a + i * stepSize);
                last = (double)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a - i * stepSize);
                    last = (double)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a + i * stepSize);
                    last = (double)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static sbyte[] Range(sbyte a, sbyte b, sbyte stepSize)
        {
            if (a == b)
                return new sbyte[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            sbyte[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new sbyte[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (sbyte)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new sbyte[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (sbyte)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new sbyte[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (sbyte)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<sbyte> EnumerableRange(sbyte a, sbyte b, sbyte stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            sbyte last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (sbyte)(a + i * stepSize);
                last = (sbyte)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (sbyte)(a - i * stepSize);
                    last = (sbyte)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (sbyte)(a + i * stepSize);
                    last = (sbyte)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static long[] Range(long n)
        {
            long[] r = new long[(int)n];
            for (int i = 0; i < r.Length; i++)
                r[i] = (long)i;
            return r;
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        ///
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static long[] Range(long a, long b)
        {
            if (a == b)
                return new long[] { };

            long[] r;

            if (b > a)
            {
                r = new long[(int)(b - a)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (long)(a++);
            }
            else
            {
                r = new long[(int)(a - b)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (long)(a--);
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<long> EnumerableRange(long n)
        {
            for (long i = 0; i < n; i++)
                yield return (long)i;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<long> EnumerableRange(long a, long b)
        {
            if (a == b)
                yield break;

            if (b > a)
            {
                int n = (int)(b - a);
                for (int i = 0; i < n; i++)
                    yield return (long)(a++);
            }
            else
            {
                int n = (int)(a - b);
                for (int i = 0; i < n; i++)
                    yield return (long)(a--);
            }
        }


        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(long a, long b, float stepSize)
        {
            if (a == b)
                return new float[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));

                r = new float[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (float)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<float> EnumerableRange(long a, long b, float stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (float)(a + i * stepSize);
                last = (float)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a - i * stepSize);
                    last = (float)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a + i * stepSize);
                    last = (float)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(long a, long b, double stepSize)
        {
            if (a == b)
                return new double[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new double[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (double)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<double> EnumerableRange(long a, long b, double stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (double)(a + i * stepSize);
                last = (double)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a - i * stepSize);
                    last = (double)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a + i * stepSize);
                    last = (double)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static long[] Range(long a, long b, long stepSize)
        {
            if (a == b)
                return new long[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            long[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new long[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (long)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new long[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (long)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new long[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (long)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<long> EnumerableRange(long a, long b, long stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            long last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (long)(a + i * stepSize);
                last = (long)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (long)(a - i * stepSize);
                    last = (long)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (long)(a + i * stepSize);
                    last = (long)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static decimal[] Range(decimal n)
        {
            decimal[] r = new decimal[(int)n];
            for (int i = 0; i < r.Length; i++)
                r[i] = (decimal)i;
            return r;
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        ///
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static decimal[] Range(decimal a, decimal b)
        {
            if (a == b)
                return new decimal[] { };

            decimal[] r;

            if (b > a)
            {
                r = new decimal[(int)(b - a)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (decimal)(a++);
            }
            else
            {
                r = new decimal[(int)(a - b)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (decimal)(a--);
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<decimal> EnumerableRange(decimal n)
        {
            for (decimal i = 0; i < n; i++)
                yield return (decimal)i;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<decimal> EnumerableRange(decimal a, decimal b)
        {
            if (a == b)
                yield break;

            if (b > a)
            {
                int n = (int)(b - a);
                for (int i = 0; i < n; i++)
                    yield return (decimal)(a++);
            }
            else
            {
                int n = (int)(a - b);
                for (int i = 0; i < n; i++)
                    yield return (decimal)(a--);
            }
        }


        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static decimal[] Range(decimal a, decimal b, decimal stepSize)
        {
            if (a == b)
                return new decimal[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            decimal[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((decimal)(b - a) / (decimal)stepSize));

                r = new decimal[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (decimal)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((decimal)(a - b) / (decimal)stepSize));
                    r = new decimal[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (decimal)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((decimal)(b - a) / (decimal)stepSize));
                    r = new decimal[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (decimal)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<decimal> EnumerableRange(decimal a, decimal b, decimal stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            decimal last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((decimal)(b - a) / (decimal)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (decimal)(a + i * stepSize);
                last = (decimal)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((decimal)(a - b) / (decimal)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (decimal)(a - i * stepSize);
                    last = (decimal)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((decimal)(b - a) / (decimal)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (decimal)(a + i * stepSize);
                    last = (decimal)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static ulong[] Range(ulong n)
        {
            ulong[] r = new ulong[(int)n];
            for (int i = 0; i < r.Length; i++)
                r[i] = (ulong)i;
            return r;
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        ///
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static ulong[] Range(ulong a, ulong b)
        {
            if (a == b)
                return new ulong[] { };

            ulong[] r;

            if (b > a)
            {
                r = new ulong[(int)(b - a)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (ulong)(a++);
            }
            else
            {
                r = new ulong[(int)(a - b)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (ulong)(a--);
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<ulong> EnumerableRange(ulong n)
        {
            for (ulong i = 0; i < n; i++)
                yield return (ulong)i;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<ulong> EnumerableRange(ulong a, ulong b)
        {
            if (a == b)
                yield break;

            if (b > a)
            {
                int n = (int)(b - a);
                for (int i = 0; i < n; i++)
                    yield return (ulong)(a++);
            }
            else
            {
                int n = (int)(a - b);
                for (int i = 0; i < n; i++)
                    yield return (ulong)(a--);
            }
        }


        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(ulong a, ulong b, float stepSize)
        {
            if (a == b)
                return new float[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));

                r = new float[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (float)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<float> EnumerableRange(ulong a, ulong b, float stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (float)(a + i * stepSize);
                last = (float)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a - i * stepSize);
                    last = (float)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a + i * stepSize);
                    last = (float)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(ulong a, ulong b, double stepSize)
        {
            if (a == b)
                return new double[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new double[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (double)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<double> EnumerableRange(ulong a, ulong b, double stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (double)(a + i * stepSize);
                last = (double)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a - i * stepSize);
                    last = (double)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a + i * stepSize);
                    last = (double)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static ulong[] Range(ulong a, ulong b, ulong stepSize)
        {
            if (a == b)
                return new ulong[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            ulong[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new ulong[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (ulong)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new ulong[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (ulong)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new ulong[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (ulong)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<ulong> EnumerableRange(ulong a, ulong b, ulong stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            ulong last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (ulong)(a + i * stepSize);
                last = (ulong)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (ulong)(a - i * stepSize);
                    last = (ulong)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (ulong)(a + i * stepSize);
                    last = (ulong)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static ushort[] Range(ushort n)
        {
            ushort[] r = new ushort[(int)n];
            for (int i = 0; i < r.Length; i++)
                r[i] = (ushort)i;
            return r;
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        ///
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static ushort[] Range(ushort a, ushort b)
        {
            if (a == b)
                return new ushort[] { };

            ushort[] r;

            if (b > a)
            {
                r = new ushort[(int)(b - a)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (ushort)(a++);
            }
            else
            {
                r = new ushort[(int)(a - b)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = (ushort)(a--);
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="n">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<ushort> EnumerableRange(ushort n)
        {
            for (ushort i = 0; i < n; i++)
                yield return (ushort)i;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<ushort> EnumerableRange(ushort a, ushort b)
        {
            if (a == b)
                yield break;

            if (b > a)
            {
                int n = (int)(b - a);
                for (int i = 0; i < n; i++)
                    yield return (ushort)(a++);
            }
            else
            {
                int n = (int)(a - b);
                for (int i = 0; i < n; i++)
                    yield return (ushort)(a--);
            }
        }


        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(ushort a, ushort b, float stepSize)
        {
            if (a == b)
                return new float[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));

                r = new float[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (float)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize));
                    r = new float[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (float)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<float> EnumerableRange(ushort a, ushort b, float stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            float last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (float)(a + i * stepSize);
                last = (float)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(a - b) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a - i * stepSize);
                    last = (float)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((float)(b - a) / (float)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (float)(a + i * stepSize);
                    last = (float)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(ushort a, ushort b, double stepSize)
        {
            if (a == b)
                return new double[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new double[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (double)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new double[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (double)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<double> EnumerableRange(ushort a, ushort b, double stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            double last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (double)(a + i * stepSize);
                last = (double)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a - i * stepSize);
                    last = (double)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (double)(a + i * stepSize);
                    last = (double)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static ushort[] Range(ushort a, ushort b, ushort stepSize)
        {
            if (a == b)
                return new ushort[] { };

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            ushort[] r;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));

                r = new ushort[steps];
                for (uint i = 0; i < r.Length; i++)
                    r[i] = (ushort)(a + i * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize));
                    r = new ushort[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (ushort)(a - i * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize));
                    r = new ushort[steps];
                    for (uint i = 0; i < r.Length; i++)
                        r[i] = (ushort)(a + i * stepSize);
                }
            }

            if (a < b)
            {
                if (r[r.Length - 1] > b)
                    r[r.Length - 1] = b;
            }
            else
            {
                if (r[r.Length - 1] > a)
                    r[r.Length - 1] = a;
            }

            return r;
        }

        /// <summary>
        ///   Enumerates through a range (like Python's xrange function).
        /// </summary>
        /// 
        /// <param name="a">The inclusive lower bound of the range.</param>
        /// <param name="b">The exclusive upper bound of the range.</param>
        /// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static IEnumerable<ushort> EnumerableRange(ushort a, ushort b, ushort stepSize)
        {
            if (a == b)
                yield break;

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException("stepSize", "stepSize must be different from zero.");

            ushort last;

            if (a < b)
            {
                if (stepSize < 0)
                    throw new ArgumentOutOfRangeException("stepSize", "If a < b, stepSize must be positive.");

                uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                for (uint i = 0; i < steps; i++)
                    yield return (ushort)(a + i * stepSize);
                last = (ushort)(a + steps * stepSize);
            }
            else
            {
                if (stepSize > 0)
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(a - b) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (ushort)(a - i * stepSize);
                    last = (ushort)(a - steps * stepSize);
                }
                else
                {
                    uint steps = (uint)System.Math.Ceiling(((double)(b - a) / (double)stepSize)) - 1;
                    for (uint i = 0; i < steps; i++)
                        yield return (ushort)(a + i * stepSize);
                    last = (ushort)(a + steps * stepSize);
                }
            }

            if (a < b)
            {
                yield return last > b ? b : last;
            }
            else
            {
                yield return last > a ? a : last;
            }
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="range">The range from where values should be created.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(this DoubleRange range)
        {
            return Range(range.Min, range.Max);
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="range">The range from where values should be created.</param>
		/// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(this DoubleRange range, double stepSize)
        {
            return Range(range.Min, range.Max, stepSize);
        }
        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="range">The range from where values should be created.</param>
		/// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(this DoubleRange range, float stepSize)
        {
            return Range(range.Min, range.Max, stepSize);
        }
        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="range">The range from where values should be created.</param>
		/// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(this DoubleRange range, byte stepSize)
        {
            return Range(range.Min, range.Max, stepSize);
        }
        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="range">The range from where values should be created.</param>
		/// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(this DoubleRange range, int stepSize)
        {
            return Range(range.Min, range.Max, stepSize);
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="range">The range from where values should be created.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(this Range range)
        {
            return Range(range.Min, range.Max);
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="range">The range from where values should be created.</param>
		/// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(this Range range, double stepSize)
        {
            return Range(range.Min, range.Max, stepSize);
        }
        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="range">The range from where values should be created.</param>
		/// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(this Range range, float stepSize)
        {
            return Range(range.Min, range.Max, stepSize);
        }
        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="range">The range from where values should be created.</param>
		/// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(this Range range, byte stepSize)
        {
            return Range(range.Min, range.Max, stepSize);
        }
        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="range">The range from where values should be created.</param>
		/// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(this Range range, int stepSize)
        {
            return Range(range.Min, range.Max, stepSize);
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="range">The range from where values should be created.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static byte[] Range(this ByteRange range)
        {
            return Range(range.Min, range.Max);
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="range">The range from where values should be created.</param>
		/// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(this ByteRange range, double stepSize)
        {
            return Range(range.Min, range.Max, stepSize);
        }
        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="range">The range from where values should be created.</param>
		/// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(this ByteRange range, float stepSize)
        {
            return Range(range.Min, range.Max, stepSize);
        }
        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="range">The range from where values should be created.</param>
		/// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static byte[] Range(this ByteRange range, byte stepSize)
        {
            return Range(range.Min, range.Max, stepSize);
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="range">The range from where values should be created.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static int[] Range(this IntRange range)
        {
            return Range(range.Min, range.Max);
        }

        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="range">The range from where values should be created.</param>
		/// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static double[] Range(this IntRange range, double stepSize)
        {
            return Range(range.Min, range.Max, stepSize);
        }
        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="range">The range from where values should be created.</param>
		/// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static float[] Range(this IntRange range, float stepSize)
        {
            return Range(range.Min, range.Max, stepSize);
        }
        /// <summary>
        ///   Creates a range vector (like NumPy's arange function).
        /// </summary>
        /// 
        /// <param name="range">The range from where values should be created.</param>
		/// <param name="stepSize">The step size to be taken between elements. 
        ///   This parameter can be negative to create a decreasing range.</param>
		///
        /// <remarks>
        /// <para>
        ///   The Range methods should be equivalent to NumPy's np.arange method, with one
        ///   single difference: when the intervals are inverted (i.e. a > b) and the step
        ///   size is negative, the framework still iterates over the range backwards, as 
        ///   if the step was negative.</para>
        /// <para>
        ///   This function never includes the upper bound of the range. For methods
        ///   that include it, please see the <see cref="Interval(int, int)"/> methods.</para>  
        /// </remarks>
        ///
        /// <seealso cref="Interval(int, int)"/>
        ///
        public static int[] Range(this IntRange range, int stepSize)
        {
            return Range(range.Min, range.Max, stepSize);
        }


        #endregion




    }

    public static class GetRangeStatic
    {
        #region GetRange

        /// <summary>
        ///   Gets the maximum and minimum values in a vector.
        /// </summary>
        /// 
        /// <param name="values">The vector whose min and max should be computed.</param>
        /// <param name="min">The minimum value in the vector.</param>
        /// <param name="max">The maximum value in the vector.</param>
        /// 
        /// <exception cref="System.ArgumentException">Raised if the array is empty.</exception>
        /// 
        public static void GetRange<T>(this T[] values, out T min, out T max)
            where T : IComparable<T>
        {
            if (values.Length == 0)
            {
                min = max = default(T);
                return;
            }

            min = max = values[0];
            for (int i = 1; i < values.Length; i++)
            {
                if (values[i].CompareTo(min) < 0)
                    min = values[i];
                if (values[i].CompareTo(max) > 0)
                    max = values[i];
            }
        }

        /// <summary>
        ///   Gets the maximum and minimum values in a matrix.
        /// </summary>
        /// 
        /// <param name="values">The vector whose min and max should be computed.</param>
        /// <param name="min">The minimum value in the vector.</param>
        /// <param name="max">The maximum value in the vector.</param>
        /// 
        /// <exception cref="System.ArgumentException">Raised if the array is empty.</exception>
        /// 
        public static void GetRange<T>(this T[,] values, out T min, out T max)
            where T : IComparable<T>
        {
            if (values.Length == 0)
            {
                min = max = default(T);
                return;
            }

            min = max = values[0, 0];
            foreach (var v in values)
            {
                if (v.CompareTo(min) < 0)
                    min = v;
                if (v.CompareTo(max) > 0)
                    max = v;
            }
        }


        /// <summary>
        ///   Gets the maximum and minimum values in a vector.
        /// </summary>
        /// 
        /// <param name="values">The vector whose min and max should be computed.</param>
        /// 
        /// <exception cref="System.ArgumentException">Raised if the array is empty.</exception>
        /// 
        public static IntRange GetRange(this int[] values)
        {
            int min, max;
            GetRange(values, out min, out max);
            return new IntRange(min, max);
        }
        /// <summary>
        ///   Gets the maximum and minimum values in a vector.
        /// </summary>
        /// 
        /// <param name="values">The vector whose min and max should be computed.</param>
        /// 
        /// <exception cref="System.ArgumentException">Raised if the array is empty.</exception>
        /// 
        public static DoubleRange GetRange(this double[] values)
        {
            double min, max;
            GetRange(values, out min, out max);
            return new DoubleRange(min, max);
        }

        /// <summary>
        ///   Gets the maximum and minimum values in a vector.
        /// </summary>
        /// 
        /// <param name="values">The vector whose min and max should be computed.</param>
        /// 
        /// <exception cref="System.ArgumentException">Raised if the array is empty.</exception>
        /// 
        public static IntRange GetRange(this int[,] values)
        {
            int min, max;
            GetRange(values, out min, out max);
            return new IntRange(min, max);
        }

        /// <summary>
        ///   Gets the maximum and minimum values in a vector.
        /// </summary>
        /// 
        /// <param name="values">The vector whose min and max should be computed.</param>
        /// 
        /// <exception cref="System.ArgumentException">Raised if the array is empty.</exception>
        /// 
        public static DoubleRange GetRange(this double[,] values)
        {
            double min, max;
            GetRange(values, out min, out max);
            return new DoubleRange(min, max);
        }

        /// <summary>
        ///   Gets the range of the values across the columns of a matrix.
        /// </summary>
        /// 
        /// <param name="value">The matrix whose ranges should be computed.</param>
        /// <param name="dimension">
        ///   Pass 0 if the range should be computed for each of the columns. Pass 1
        ///   if the range should be computed for each row. Default is 0.
        /// </param>
        /// 
        public static DoubleRange[] GetRange(this double[,] value, int dimension)
        {
            int rows = value.GetLength(0);
            int cols = value.GetLength(1);
            DoubleRange[] ranges;

            if (dimension == 0)
            {
                ranges = new DoubleRange[cols];

                for (int j = 0; j < ranges.Length; j++)
                {
                    double max = value[0, j];
                    double min = value[0, j];

                    for (int i = 0; i < rows; i++)
                    {
                        if (value[i, j] > max)
                            max = value[i, j];
                        if (value[i, j] < min)
                            min = value[i, j];
                    }

                    ranges[j] = new DoubleRange(min, max);
                }
            }
            else
            {
                ranges = new DoubleRange[rows];

                for (int j = 0; j < ranges.Length; j++)
                {
                    double max = value[j, 0];
                    double min = value[j, 0];

                    for (int i = 0; i < cols; i++)
                    {
                        if (value[j, i] > max)
                            max = value[j, i];
                        if (value[j, i] < min)
                            min = value[j, i];
                    }

                    ranges[j] = new DoubleRange(min, max);
                }
            }

            return ranges;
        }
        #endregion
    }
}
