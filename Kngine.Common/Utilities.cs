using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;

namespace Kngine
{
    internal struct MEMORYSTATUSEX
    {
        internal uint dwLength;
        internal uint dwMemoryLoad;
        internal ulong ullTotalPhys;
        internal ulong ullAvailPhys;
        internal ulong ullTotalPageFile;
        internal ulong ullAvailPageFile;
        internal ulong ullTotalVirtual;
        internal ulong ullAvailVirtual;
        internal ulong ullAvailExtendedVirtual;
        internal void Init()
        {
            this.dwLength = checked((uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)));
        }
    }

    public static class Utilities
    {
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        public static long FreeMemory
        {
            get
            {
                try
                {
                    var MEMSTATUS = default(MEMORYSTATUSEX);
                    MEMSTATUS.Init();
                    GlobalMemoryStatusEx(ref MEMSTATUS);
                    long mem = (long)MEMSTATUS.ullAvailPhys;
                    return mem;
                }
                catch
                {
                    return -1;
                }
            }
        }

        /**/

        /// <summary>
        /// Serialize using .NET Binary Format
        /// </summary>
        public static byte[] Serialize(object obj, bool compress = false)
        {
            IFormatter binFormater = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            return Serialize(binFormater, obj, compress);
        }

        /// <summary>
        /// Deserialize using .NET Binary Format
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static object Deserialize(byte[] bytes, bool decompress = false)
        {
            IFormatter binFormater = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            return Deserialize(binFormater, bytes, decompress);
        }

        /// <summary>
        /// Serialize using .NET Binary Format
        /// </summary>
        public static byte[] Serialize(this IFormatter s, object obj, bool compress = false)
        {
            using (MemoryStream mem = new MemoryStream())
            {
                s.Serialize(mem, obj);
                byte[] Bs = new byte[mem.Length];

                mem.Seek(0, SeekOrigin.Begin);
                mem.Read(Bs, 0, Bs.Length);

                if (compress)
                    return new Kngine.IO.Compression.AcedDeflator().Compress(Bs, IO.Compression.AcedCompressionLevel.Fast);
                else
                    return Bs;
            }
        }

        /// <summary>
        /// Deserialize using .NET Binary Format
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static object Deserialize(this IFormatter s, byte[] bytes, bool decompress = false)
        {
            if (decompress) bytes = new Kngine.IO.Compression.AcedInflator().Decompress(bytes);
            using (MemoryStream mem = new MemoryStream(bytes))
                return s.Deserialize(mem);

        }

        /// <summary>
        /// Serialize using .NET Binary Format and then convert it to string
        /// </summary>
        /// <param name="Graph"></param>
        /// <returns></returns>
        public static string ObjectToString(object Graph, bool compress = false)
        {
            using (MemoryStream MS = new MemoryStream())
            {
                BinaryFormatter BF = new BinaryFormatter();
                BF.Serialize(MS, Graph);

                byte[] Bs = new byte[MS.Length];
                MS.Seek(0, SeekOrigin.Begin);
                MS.Read(Bs, 0, Bs.Length);
                if (compress) Bs = new Kngine.IO.Compression.AcedDeflator().Compress(Bs, IO.Compression.AcedCompressionLevel.Fast);

                StringBuilder SB = new StringBuilder(Bs.Length);
                SB.Append(Bs[0]);
                for (int i = 1; i < Bs.Length; i++)
                    SB.Append(";" + Bs[i]);

                return SB.ToString();
            }
        }

        /// <summary>
        /// Deserialize using .NET Binary Format and after convert it to bytes from string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static object StringToObject(string str, bool decompress = false)
        {
            string[] x = str.Split(';');
            byte[] bytes = new byte[x.Length];
            for (int i = 0; i < x.Length; i++)
                bytes[i] = byte.Parse(x[i]);
            if (decompress) bytes = new Kngine.IO.Compression.AcedInflator().Decompress(bytes);

            using (MemoryStream MS = new MemoryStream(bytes))
            {
                MS.Seek(0, SeekOrigin.Begin);
                BinaryFormatter BF = new BinaryFormatter();
                return BF.Deserialize(MS);
            }
        }

        /// <summary>
        /// Serialize using Protocol Buffer
        /// </summary>
        public static byte[] ProtoBufSerialize<T>(T obj)
        {
            using (MemoryStream mem = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize<T>(mem, obj);
                byte[] Bs = new byte[mem.Length];

                mem.Seek(0, SeekOrigin.Begin);
                mem.Read(Bs, 0, Bs.Length);
                return Bs;
            }
        }

        /// <summary>
        /// Deserialize using Protocol Buffer
        /// </summary>
        public static T ProtoBufDeserialize<T>(byte[] bytes)
        {
            using (MemoryStream mem = new MemoryStream(bytes))
                return ProtoBuf.Serializer.Deserialize<T>(mem);
        }

        /**/

        public static string BytesToString(byte[] bytes)
        {
            StringBuilder SB = new StringBuilder(bytes.Length);
            SB.Append(bytes[0]);
            for (int i = 1; i < bytes.Length; i++)
                SB.Append(";" + bytes[i]);

            return SB.ToString();
        }

        public static byte[] StringToBytes(string str)
        {
            string[] x = str.Split(';');
            byte[] bytes = new byte[x.Length];
            for (int i = 0; i < x.Length; i++)
                bytes[i] = byte.Parse(x[i]);

            return bytes;
        }

        /**/

        public static T DeserializeXML<T>(this string fileName)
        {
            System.Xml.Serialization.XmlSerializer SittingSeralizer = new System.Xml.Serialization.XmlSerializer(typeof(T));

            using (FileStream FS = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                return (T)SittingSeralizer.Deserialize(FS);
        }

        public static void SerializeXML<T>(this string fileName, T obj)
        {
            System.Xml.Serialization.XmlSerializer SittingSeralizer = new System.Xml.Serialization.XmlSerializer(typeof(T));

            using (FileStream FS = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                SittingSeralizer.Serialize(FS, obj);
        }

        /**/

        public static bool IsDirectoryExists(this string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return false;
                var x = Directory.GetDirectories(path);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /**/

        internal static T InitObjectAndThrowException<T>()
            where T : class
        {
            T R;
            try
            {
                R = Activator.CreateInstance<T>();
                return R;
            }
            catch
            {
            }

            try
            {
                R = FormatterServices.GetUninitializedObject(typeof(T)) as T;
                return R;
            }
            catch
            {
                throw;
            }

        }

        /// <summary>
        /// This method initalize object 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T InitObject<T>()
            where T : class
        {
            T R;
            try
            {
                R = Activator.CreateInstance<T>();
                return R;
            }
            catch
            {
            }

            try
            {
                R = FormatterServices.GetUninitializedObject(typeof(T)) as T;
                return R;
            }
            catch
            {
                throw;
            }

        }
    }
}
