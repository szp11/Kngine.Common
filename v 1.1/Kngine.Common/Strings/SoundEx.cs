using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kngine.Strings
{
    public static class SoundEx
    {
        public static char ENEncodeChar(char c)
        {
            switch (char.ToLower(c))
            {
                case 'b':
                case 'f':
                case 'p':
                case 'v':
                    return '1';
                case 'c':
                case 'g':
                case 'j':
                case 'k':
                case 'q':
                case 's':
                case 'x':
                case 'z':
                    return '2';
                case 'd':
                case 't':
                    return '3';
                case 'l':
                    return '4';
                case 'm':
                case 'n':
                    return '5';
                case 'r':
                    return '6';
                default:
                    return '-';
            }
        }

        public static char AREncodeChar(char c)
        {
            switch (c)
            {
                case 'ا':
                case 'أ':
                case 'إ':
                case 'آ':
                case 'ح':
                case 'خ':
                case 'ه':
                case 'ع':
                case 'غ':
                case 'ش':
                case 'و':
                case 'ي':
                    return '0';
                case 'ف':
                case 'ب':
                    return '1';
                case 'ج':
                case 'ز':
                case 'س':
                case 'ص':
                case 'ظ':
                case 'ق':
                case 'ك':
                    return '2';
                case 'ت':
                case 'ث':
                case 'د':
                case 'ذ':
                case 'ض':
                case 'ط':
                    return '3';
                case 'ل':
                    return '4';
                case 'م':
                case 'ن':
                    return '5';
                case 'ر':
                    return '6';
                default:
                    return '-';
            }
        }


        public static string GenerateSoundEx(this string s, bool ignoreFirstChar)
        {
            if (IsArabic(s))
                return ARGenerateSoundEx(s, ignoreFirstChar);
            else
                return ENGenerateSoundEx(s, ignoreFirstChar);
        }

        public static string ARGenerateSoundEx(this string s, bool ignoreFirstChar)
        {
            StringBuilder output = new StringBuilder();

            if (s.Length > 0)
            {

                if (!ignoreFirstChar)
                    output.Append(Char.ToUpper(s[0]));

                // Stop at a maximum of 4 characters
                for (int i = 1; i < s.Length && output.Length < 8; i++)
                {
                    var c = AREncodeChar(s[i]);
                    if (c == '-') continue;

                    // Ignore duplicated chars, except a duplication with the first char
                    if (i == 1)
                    {
                        output.Append(c);
                    }
                    else if (c != ENEncodeChar(s[i - 1]))
                    {
                        output.Append(c);
                    }
                }

                if (output.Length == 0) return null;

                // Pad with zeros
                for (int i = output.Length; i < 4; i++)
                {
                    output.Append("0");
                }
            }

            return output.ToString();
        }

        public static string ENGenerateSoundEx(this string s, bool ignoreFirstChar)
        {
            StringBuilder output = new StringBuilder();

            if (s.Length > 0)
            {

                if (!ignoreFirstChar)
                    output.Append(Char.ToUpper(s[0]));

                // Stop at a maximum of 4 characters
                for (int i = 1; i < s.Length && output.Length < 8; i++)
                {
                    var c = ENEncodeChar(s[i]);
                    if (c == '-') continue;

                    // Ignore duplicated chars, except a duplication with the first char
                    if (i == 1)
                    {
                        output.Append(c);
                    }
                    else if (c != ENEncodeChar(s[i - 1]))
                    {
                        output.Append(c);
                    }
                }

                if (output.Length == 0) return null;

                // Pad with zeros
                for (int i = output.Length; i < 4; i++)
                {
                    output.Append("0");
                }
            }

            return output.ToString();
        }

        /*///////////////////////////////////////////////////////////////////////////////////////////////////*/

        public static bool IsArabic(string s)
        {
            int result = 0;
            for (int i = 0; i < s.Length; i++)
                result = (IsArabic(s[i]) ? result + 1 : result);

            return result > (s.Length / 2);
        }

        public static bool IsArabic(char c)
        {
            switch (c)
            {
                case 'أ':
                case 'ا':
                case 'إ':
                case 'آ':
                case 'ح':
                case 'خ':
                case 'ه':
                case 'ع':
                case 'غ':
                case 'ش':
                case 'و':
                case 'ي':
                case 'ف':
                case 'ب':
                case 'ج':
                case 'ز':
                case 'س':
                case 'ص':
                case 'ظ':
                case 'ق':
                case 'ك':
                case 'ت':
                case 'ث':
                case 'د':
                case 'ذ':
                case 'ض':
                case 'ط':
                case 'ل':
                case 'م':
                case 'ن':
                case 'ر':
                    return true;
                default: return false;
            }
        }
    }

    public static class StringDistance
    {
        /// <summary>
        /// Calculates the Levenshtein-distance of two strings.
        /// </summary>
        /// <param name="src">
        /// 1. string
        /// </param>
        /// <param name="dest">
        /// 2. string
        /// </param>
        /// <returns>
        /// Levenshstein-distance
        /// </returns>
        /// <remarks>
        /// See 
        /// <a href='http://en.wikipedia.org/wiki/Levenshtein_distance'>
        /// http://en.wikipedia.org/wiki/Levenshtein_distance
        /// </a>
        /// </remarks>
        public static int LevenshteinDistance(this string src, string dest)
        {
            int[,] d = new int[src.Length + 1, dest.Length + 1];
            int i, j, cost;
            char[] str1 = src.ToCharArray();
            char[] str2 = dest.ToCharArray();

            for (i = 0; i <= str1.Length; i++)
            {
                d[i, 0] = i;
            }
            for (j = 0; j <= str2.Length; j++)
            {
                d[0, j] = j;
            }
            for (i = 1; i <= str1.Length; i++)
            {
                for (j = 1; j <= str2.Length; j++)
                {

                    if (str1[i - 1] == str2[j - 1])
                        cost = 0;
                    else
                        cost = 1;

                    d[i, j] =
                        Math.Min(
                            d[i - 1, j] + 1,					// Deletion
                            Math.Min(
                                d[i, j - 1] + 1,				// Insertion
                                d[i - 1, j - 1] + cost));		// Substitution

                    if ((i > 1) && (j > 1) && (str1[i - 1] == str2[j - 2]) && (str1[i - 2] == str2[j - 1]))
                    {
                        d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
                    }
                }
            }

            return d[str1.Length, str2.Length];
        }
    }
}
