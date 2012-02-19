using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using Kngine.Strings;

namespace Test
{
    public static class StringOpTest
    {
        // file to test change it
        private static string s107 = File.ReadAllText(@"c:\d0.txt");
        private static string s700 = File.ReadAllText(@"c:\d1.txt").Substring(0, 700);
        private static string s1000 = File.ReadAllText(@"c:\d1.txt");
        private static string s2000 = File.ReadAllText(@"c:\d2.txt").Substring(0, 2000);
        private static string s4000 = File.ReadAllText(@"c:\d2.txt");
        private static string s24000 = File.ReadAllText(@"c:\doc data.txt");


        public static void MeasureCorrectess()
        {
            MeasureCorrectess(s107, "[");
            MeasureCorrectess(s107, "[[");
            MeasureCorrectess(s107, "IS A");
            MeasureCorrectess(s107, "(định hướnG)");
            MeasureCorrectess(s107, "Kiseki/No.1 &quot;&amp;#160;");

            MeasureCorrectess(s700, "[");
            MeasureCorrectess(s700, "[[");
            MeasureCorrectess(s700, "IS A");
            MeasureCorrectess(s700, "(định hướnG)");
            MeasureCorrectess(s700, "Kiseki/No.1 &quot;&amp;#160;");

            MeasureCorrectess(s2000, "[");
            MeasureCorrectess(s2000, "[[");
            MeasureCorrectess(s2000, "IS A");
            MeasureCorrectess(s2000, "(định hướnG)");
            MeasureCorrectess(s2000, "Kiseki/No.1 &quot;&amp;#160;");

            MeasureCorrectess(s24000, "[");
            MeasureCorrectess(s24000, "[[");
            MeasureCorrectess(s24000, "IS A");
            MeasureCorrectess(s24000, "(định hướnG)");
            MeasureCorrectess(s24000, "Kiseki/No.1 &quot;&amp;#160;");


        }

        private static void MeasureCorrectess(string source, string pattern)
        {
            MeasureCorrectess(source, pattern, false);
            MeasureCorrectess(source, pattern, true);
        }

        private static void MeasureCorrectess(string source, string pattern, bool ignoreCase)
        {
            int index = -1;
            for (int i = 0; i < 10; i++)
            {
                int strIndex = -1, FIndex = -1;
                if (ignoreCase)
                {
                    strIndex = source.IndexOf(pattern, index + 1, StringComparison.OrdinalIgnoreCase);
                    FIndex = StringOps.FastIndexOfIgnoreCase(source, pattern, index + 1);
                }
                else
                {
                    strIndex = source.IndexOf(pattern, index + 1, StringComparison.Ordinal);
                    FIndex = StringOps.FastIndexOf(source, pattern, index + 1);
                }
                if (FIndex != strIndex)
                {

                }
                index = strIndex;
            }
        }

        /*/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////*/

        public static void MeasurePerformance()
        {
            MeasurePerformance("[[a");
            MeasurePerformance("[");
            MeasurePerformance("[[");
            MeasurePerformance("IS A");
            MeasurePerformance("(định hướnG)");
            MeasurePerformance("Kiseki/No.1 &quot;&amp;#160;");
        }

        private static void MeasurePerformance(string pattern)
        {
            Action<string> NetIndexOf = (s) => { s.IndexOf(pattern); };
            Action<string> OurIndexOf = (s) => { StringOps.FastIndexOf(s, pattern); };
            var x = pattern.Substring(0);
            x = null;

            Measure(".NET Index Of - str size 107 - " + x + " : ", () => { NetIndexOf(s107); });
            Measure(".Our Index Of - str size 107 - " + x + " : ", () => { OurIndexOf(s107); });

            Console.WriteLine();

            Measure(".NET Index Of - str size 700 - " + x + " : ", () => { NetIndexOf(s700); });
            Measure(".Our Index Of - str size 700 - " + x + " : ", () => { OurIndexOf(s700); });

            Console.WriteLine();

            Measure(".NET Index Of - str size 1000 - " + x + " : ", () => { NetIndexOf(s1000); });
            Measure(".Our Index Of - str size 1000 - " + x + " : ", () => { OurIndexOf(s1000); });

            Console.WriteLine();

            Measure(".NET Index Of - str size 2000 - " + x + " : ", () => { NetIndexOf(s2000); });
            Measure(".Our Index Of - str size 2000 - " + x + " : ", () => { OurIndexOf(s2000); });

            Console.WriteLine();

            Measure(".NET Index Of - str size 4000 - " + x + " : ", () => { NetIndexOf(s4000); });
            Measure(".Our Index Of - str size 4000 - " + x + " : ", () => { OurIndexOf(s4000); });

            Console.WriteLine();

            Measure(".NET Index Of - str size 24000 - " + x + " : ", () => { NetIndexOf(s24000); });
            Measure(".Our Index Of - str size 24000 - " + x + " : ", () => { OurIndexOf(s24000); });

        }

        /*/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////*/

        private static void Measure(string name, Action a)
        {
            a();

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < 2; i++)
                a();

            sw.Stop();
            Console.WriteLine(name + "- 2 :: " + sw.ElapsedTicks);


            sw = Stopwatch.StartNew();
            for (int i = 0; i < 12; i++)
                a();

            sw.Stop();
            Console.WriteLine(name + "- 12 :: " + sw.ElapsedTicks);


            sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
                a();

            sw.Stop();
            Console.WriteLine(name + "- 100 :: " + sw.ElapsedTicks);


            sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
                a();

            sw.Stop();
            Console.WriteLine(name + "- 1000 :: " + sw.ElapsedTicks);

        }
    }

}
