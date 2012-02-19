using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kngine.LINQ
{
    public static class ListProcessor
    {
        public delegate bool FuncX<in T1, TResult>(T1 arg1, out TResult arg2);

        public static List<TResult> WhereSelect<TSource, TResult>(this List<TSource> list, FuncX<TSource, TResult> selector)
        {
            List<TResult> TR = new List<TResult>(list.Count);

            for (int i = 0; i < list.Count; i++)
            {
                TResult result;
                if (selector(list[i], out result)) TR.Add(result);
            }

            return TR;
        }

        public static LinkedList<TResult> WhereSelect<TSource, TResult>(this LinkedList<TSource> list, FuncX<TSource, TResult> selector)
        {
            LinkedList<TResult> TR = new LinkedList<TResult>();

            foreach (var item in list)
            {
                TResult result;
                if (selector(item, out result)) TR.AddLast(result);
            }

            return TR;
        }

        public static LinkedList<TResult> WhereSelect<TSource, TResult>(this IEnumerable<TSource> list, FuncX<TSource, TResult> selector)
        {
            LinkedList<TResult> TR = new LinkedList<TResult>();

            foreach (var item in list)
            {
                TResult result;
                if (selector(item, out result)) TR.AddLast(result);
            }

            return TR;
        }

        public static TResult WhereSelectMax<TSource, TResult>(this LinkedList<TSource> list, FuncX<TSource, TResult> selector, Func<TResult, double> selector2)
        {
            double max = double.MinValue;
            var TR = default(TResult);

            foreach (var item in list)
            {
                TResult result;
                if (selector(item, out result))
                {
                    var tmp = selector2(result);
                    if (tmp > max)
                    {
                        TR = result;
                        max = tmp;
                    }
                }
            }

            return TR;
        }

        public static TResult WhereSelectMin<TSource, TResult>(this LinkedList<TSource> list, FuncX<TSource, TResult> selector, Func<TResult, double> selector2)
        {
            double max = double.MaxValue;
            var TR = default(TResult);

            foreach (var item in list)
            {
                TResult result;
                if (selector(item, out result))
                {
                    var tmp = selector2(result);
                    if (tmp < max)
                    {
                        TR = result;
                        max = tmp;
                    }
                }
            }

            return TR;
        }

        public static TResult WhereSelectMax<TSource, TResult>(this List<TSource> list, FuncX<TSource, TResult> selector, Func<TResult, double> selector2)
        {
            double max = double.MinValue;
            var TR = default(TResult);

            foreach (var item in list)
            {
                TResult result;
                if (selector(item, out result))
                {
                    var tmp = selector2(result);
                    if (tmp > max)
                    {
                        TR = result;
                        max = tmp;
                    }
                }
            }

            return TR;
        }

        public static TResult WhereSelectMin<TSource, TResult>(this List<TSource> list, FuncX<TSource, TResult> selector, Func<TResult, double> selector2)
        {
            double max = double.MaxValue;
            var TR = default(TResult);

            foreach (var item in list)
            {
                TResult result;
                if (selector(item, out result))
                {
                    var tmp = selector2(result);
                    if (tmp < max)
                    {
                        TR = result;
                        max = tmp;
                    }
                }
            }

            return TR;
        }

        public static string ToString(this List<string> list)
        {
            StringBuilder SB = new StringBuilder();

            for (int i = 0; i < list.Count; i++)
                SB.Append(list[i] + ", ");

            SB.Length -= 2;
            return SB.ToString();
        }

    }
}
