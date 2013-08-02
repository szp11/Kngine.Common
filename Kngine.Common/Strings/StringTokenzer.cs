using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Kngine.Strings
{
    /// <summary>
    /// A String Tokenizer that accepts Strings as source and delimiter. Only 1 delimiter is supported (either String or char[]).
    /// </summary>
    public static class StringTokenizer
    {
        public static char[] Delimiters = new char[] { ' ', '\t', '\r', '\n', ',', ',', '.', ';', ':', '\'', '\"', '!', '@', '…', '?', '$', '%', '^', '&', '*', '_', '-', '=', '+', '`', '~', '/', '\\', '(', ')', '[', ']', '{', '}', '<', '>' };

        public static List<string> Tokenize(this string str, bool removeLessThenTwoTokens = false, HashSet<string> wordsToRemove = null)
        {
            var tmp = str.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);

            List<string> tmp2;
            if (wordsToRemove != null)
            {
                tmp2 = new List<string>(tmp.Length);
                foreach (var item in tmp)
                    if (!wordsToRemove.Contains(item)) tmp2.Add(item);
            }
            else
                tmp2 = tmp.ToList();

            if (removeLessThenTwoTokens)
            {
                tmp2.RemoveAll(o => o.Length < 2);
                return tmp2;
            }
            else
                return tmp.ToList();
        }

        public static string TokenizeAndBuildStringWithoutSpaces(this string str, bool removeLessThenTwoTokens = false, HashSet<string> wordsToRemove = null)
        {
            var x = Tokenize(str, removeLessThenTwoTokens, wordsToRemove);
            StringBuilder sb = new StringBuilder(str.Length);
            foreach (var item in x)
                sb.Append(item);

            return sb.ToString();
        }

    }
}
