using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kngine.Strings
{
    /// <summary>
    /// Fast String Operations
    /// </summary>
    public unsafe static class StringOps
    {

        /// <summary>
        /// Index of - this method is faster than .NET 4.0 imp 
        /// </summary>
        /// <param name="source">Source String</param>
        /// <param name="pattern">Pattern to find</param>
        /// <returns>Index of the first occurrence</returns>
        public static int FastIndexOf(this string source, string pattern, int startIndex = 0)
        {
            if (pattern == null) throw new ArgumentNullException();
            if (pattern.Length == 0) return 0;
            if (pattern.Length > source.Length) return -1;
            if (pattern.Length == 1) return source.IndexOf(pattern[0], startIndex);

            bool found;
            int limit = source.Length - pattern.Length + 1;

            // Store the first 2 characters of "pattern"
            char c0 = pattern[0];
            char c1 = pattern[1];
            // Find the first occurrence of the first character

            int first = source.IndexOf(c0, startIndex, limit - startIndex);
            while (first != -1)
            {
                // Check if the following character is the same like
                // the 2nd character of "pattern"
                if (source[first + 1] != c1)
                {
                    first = source.IndexOf(c0, ++first, limit - first);
                    continue;
                }
                // Check the rest of "pattern" (starting with the 3rd character)
                found = true;
                for (int j = 2; j < pattern.Length; j++)
                    if (source[first + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                // If the whole word was found, return its index, otherwise try again
                if (found) return first;
                first = source.IndexOf(c0, ++first, limit - first);
            }
            return -1;
        }

        /// <summary>
        /// Index Of - this method is faster than .NET 4.0 in 95% of the cases 
        /// </summary>
        /// <param name="source">Source String</param>
        /// <param name="pattern">Pattern to find</param>
        /// <returns>Index of the first occurrence</returns>
        public static int FastIndexOfIgnoreCase(this string source, string pattern, int startIndex = 0)
        {
            if (pattern == null) throw new ArgumentNullException();
            if (pattern.Length == 0) return 0;
            if (pattern.Length > source.Length) return -1;
            if (pattern.Length == 1) return source.ToLower().IndexOf(char.ToLower(pattern[0]), startIndex);

            bool requireToLower = false;
            for (int i = 0; i < pattern.Length; i++)
            {
                if (char.IsLetter(pattern[i]))
                {
                    requireToLower = true;
                    break;
                }
            }
            if (!requireToLower) return FastIndexOf(source, pattern, startIndex);


            bool found;
            int limit = source.Length - pattern.Length + 1;

            source = source.ToLower();

            // Store the first 2 characters of "pattern"
            char c0 = char.ToLower(pattern[0]);
            char c1 = char.ToLower(pattern[1]);
            // Find the first occurrence of the first character

            int first = source.IndexOf(c0, startIndex, limit - startIndex);
            while (first != -1)
            {
                // Check if the following character is the same like
                // the 2nd character of "pattern"
                if (source[first + 1] != c1)
                {
                    first = source.IndexOf(c0, ++first, limit - first);
                    continue;
                }
                // Check the rest of "pattern" (starting with the 3rd character)
                found = true;
                for (int j = 2; j < pattern.Length; j++)
                    if (source[first + j] != char.ToLower(pattern[j]))
                    {
                        found = false;
                        break;
                    }
                // If the whole word was found, return its index, otherwise try again
                if (found) return first;
                first = source.IndexOf(c0, ++first, limit - first);
            }
            return -1;
        }


        /// <summary>
        /// Index of - this method is faster than .NET 4.0 imp. This method try to find the pattern only once.
        /// </summary>
        /// <param name="source">Source String</param>
        /// <param name="pattern">Pattern to find</param>
        /// <returns>Index of the first occurrence</returns>
        public static int UnsafeFastIndexOfForTwoLetters(this string source, string pattern, int startIndex = 0)
        {
            var sl = source.Length;
            if (startIndex == sl) return -1;
            int limit = sl - pattern.Length - startIndex + 1;

            int first = source.IndexOf(pattern[0], startIndex, limit);
            if (first > -1 && source[first + 1] == pattern[1]) return first;
            return -1;
        }

        /// <summary>
        /// Index of - this method is faster than .NET 4.0 imp. This method try to find the pattern only once.
        /// </summary>
        /// <param name="source">Source String</param>
        /// <param name="pattern">Pattern to find</param>
        /// <returns>Index of the first occurrence</returns>
        public static int UnsafeFastIndexOfForThreeLetters(this string source, string pattern, int startIndex = 0)
        {
            var sl = source.Length;
            if (startIndex == sl) return -1;
            int limit = sl - pattern.Length - startIndex + 1;

            int first = source.IndexOf(pattern[0], startIndex, limit);
            if (first > -1 && source[first + 1] == pattern[1] && source[first + 2] == pattern[2]) return first;
            return -1;
        }

        /*////////////////////////////////////////////////////////////////////////////////////////////////////////////////*/

        /// <summary>
        /// Split string by charctor
        /// </summary>
        /// <param name="s">String to be split</param>
        /// <param name="c">char to split by</param>
        public static List<string> Split(this string s, char c)
        {
            int l = s.Length;
            int i = 0, j = s.IndexOf(c, 0, l);
            if (j == -1) return null;

            var q = new List<string>(l / 4);
            while (j != -1)
            {
                if (j - i > 0) q.Add(s.Substring(i, j - i));

                i = j + 1;
                j = s.IndexOf(c, i, l - i);
            }

            if (i < l) q.Add(s.Substring(i, l - i));
            return q;
        }

        /// <summary>
        /// Split string by charctor
        /// </summary>
        /// <param name="s">String to be split</param>
        /// <param name="c">char to split by</param>
        public static List<string> Split(this string s, char c, int count)
        {
            int l = s.Length;
            int i = 0, j = s.IndexOf(c, 0, l);
            if (j == -1) return null;

            var q = new List<string>(count);
            while (j != -1)
            {
                if (j - i > 0) q.Add(s.Substring(i, j - i));

                i = j + 1;
                j = s.IndexOf(c, i, l - i);
            }

            if (i < l) q.Add(s.Substring(i, l - i));
            return q;
        }

        /// <summary>
        /// Split string by charctor
        /// </summary>
        /// <param name="s">String to be split</param>
        /// <param name="c">char to split by</param>
        public static string[] SplitInArray(this string s, char c, int count)
        {
            int l = s.Length;
            int i = 0, j = s.IndexOf(c, 0, l);
            if (j == -1) return null;

            int index = -1;
            var q = new string[count];
            while (j != -1)
            {
                if (j - i > 0) q[++index] = s.Substring(i, j - i);

                i = j + 1;
                j = s.IndexOf(c, i, l - i);
            }

            if (i < l) q[++index] = s.Substring(i, l - i);
            return q;
        }

        /// <summary>
        /// Split string by charctor
        /// </summary>
        /// <param name="s">String to be split</param>
        /// <param name="c">char to split by</param>
        public static short[] SplitInArrayInt16(this string s, char c, int count)
        {
            int l = s.Length;
            int i = 0, j = s.IndexOf(c, 0, l);
            if (j == -1) return null;

            int index = -1;
            var q = new short[count];
            while (j != -1)
            {
                if (j - i > 0) q[++index] = (short)ParseTrustedUnsafe(s, i, j);

                i = j + 1;
                j = s.IndexOf(c, i, l - i);
            }

            if (i < l) q[++index] = (short)ParseTrustedUnsafe(s, i, l);
            return q;
        }

        /// <summary>
        /// Split string by charctor
        /// </summary>
        /// <param name="s">String to be split</param>
        /// <param name="c">char to split by</param>
        public static List<string> SplitKeepEmpty(this string s, char c)
        {
            int l = s.Length;
            int i = 0, j = s.IndexOf(c, 0, l);
            if (j == -1) return null;

            var q = new List<string>(l / 4);
            while (j != -1)
            {
                if (j - i >= 0) q.Add(s.Substring(i, j - i));

                i = j + 1;
                j = s.IndexOf(c, i, l - i);
            }

            if (i <= l) q.Add(s.Substring(i, l - i));
            return q;
        }

        /*////////////////////////////////////////////////////////////////////////////////////////////////////////////////*/

        /// <summary>
        /// Split string by charctor. This method try to find the pattern only once.
        /// </summary>
        /// <param name="s">String to be split</param>
        /// <param name="c">char to split by</param>
        /// <remarks>If we didn't c in the string s, we will return list of the s</remarks>
        public static List<string> SplitByTwoLetter(this string s, string c)
        {
            int l = s.Length;
            int i = 0, j = UnsafeFastIndexOfForTwoLetters(s, c, 0);
            if (j == -1) return new List<string>(1) { s };

            var q = new List<string>(l / 4);
            while (j != -1)
            {
                if (j - i >= 0) q.Add(s.Substring(i, j - i));

                i = j + 2;
                j = UnsafeFastIndexOfForTwoLetters(s, c, i);
            }

            if (i <= l) q.Add(s.Substring(i, l - i));
            return q;
        }

        /// <summary>
        /// Split string by charctor. This method try to find the pattern only once.
        /// </summary>
        /// <param name="s">String to be split</param>
        /// <param name="c">char to split by</param>
        /// <remarks>If we didn't c in the string s, we will return list of the s</remarks>
        public static List<string> SplitByTwoLetter(this string s, string c, int count)
        {
            int l = s.Length;
            int i = 0, j = UnsafeFastIndexOfForTwoLetters(s, c, 0);
            if (j == -1) return new List<string>(1) { s };

            var q = new List<string>(count);
            while (j != -1)
            {
                if (j - i > 0) q.Add(s.Substring(i, j - i));

                i = j + 2;
                j = UnsafeFastIndexOfForTwoLetters(s, c, i);
            }

            if (i < l) q.Add(s.Substring(i, l - i));
            return q;
        }

        /*////////////////////////////////////////////////////////////////////////////////////////////////////////////////*/
        

        /// <summary>
        /// Split string by charctor
        /// </summary>
        /// <param name="s">String to be split</param>
        /// <param name="c">char to split by</param>
        public static void SplitInPlace(this string s, char c, out string s1, out string s2)
        {
            int l = s.Length;
            int j = s.IndexOf(c, 0, l);
            if (j == -1)
            {
                s1 = s2 = null;
                return;
            }
            s1 = s.Substring(0, j);
            s2 = s.Substring(j + 1);
        }

        /// <summary>
        /// Split string by charctor
        /// </summary>
        /// <param name="s">String to be split</param>
        /// <param name="c">char to split by</param>
        public static void SplitInPlace(this string s, char c, out string s1, out string s2, out string s3)
        {
            int l = s.Length;
            int j = s.IndexOf(c, 0, l);
            if (j == -1)
            {
                s1 = s2 = s3 = null;
                return;
            }
            s1 = s.Substring(0, j);
            
            int j2 = s.IndexOf(c, ++j);
            s2 = s.Substring(j, j2 - j);
            s3 = s.Substring(j2 + 1);
        }


        /*////////////////////////////////////////////////////////////////////////////////////////////////////////////////*/

        public static string ToUpperAndLower(this string str)
        {
            StringBuilder sb = new StringBuilder();
            str = str.Trim();
            sb.Append(char.ToUpper(str[0]));
            for (int i = 1; i < str.Length; i++)
            {
                var oc = str[i - 1];
                var cc = str[i];
                if (oc == ' ')
                    sb.Append(char.ToUpper(cc));
                else
                    sb.Append(char.ToLower(cc));
            }

            return sb.ToString();




        }

        /*////////////////////////////////////////////////////////////////////////////////////////////////////////////////*/

        public static unsafe int ParseTrustedUnsafe(string str, int start, int end)
        {
            unsafe
            {
                Int32 result = 0;
                Int32 length = end - start;
                Boolean isNegative = false;
                fixed (Char* startChar = str)
                {
                    Byte* currentChar = ((Byte*)startChar) + (start * 2);
                    if (*currentChar == 0x2D)
                    {
                        isNegative = true;
                        currentChar += 2;
                        length--;
                    }
                    else if (*currentChar == 0x2B)
                    {
                        currentChar += 2;
                        length--;
                    }
                    while (length >= 4)
                    {
                        result = (result * 10) + (*currentChar - 0x30);
                        result = (result * 10) + (*(currentChar + 2) - 0x30);
                        result = (result * 10) + (*(currentChar + 4) - 0x30);
                        result = (result * 10) + (*(currentChar + 6) - 0x30);

                        currentChar += 8;
                        length -= 4;
                    }
                    while (length > 0)
                    {
                        result = (result * 10) + (*currentChar - 0x30);

                        currentChar += 2;
                        length -= 1;
                    }
                }
                return isNegative ? -result : result;
            }


        }
    }
}
