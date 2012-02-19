using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kngine.Strings
{
    /// <summary>
    /// Fast String Operations
    /// </summary>
    public static class StringOps
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
    }
}
