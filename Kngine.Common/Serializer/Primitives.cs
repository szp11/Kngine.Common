using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections.Generic;

namespace Kngine.Serializer
{
    public unsafe static class Primitives
    {
        static UTF8Encoding mEncoding = new UTF8Encoding(false, true);

        /*/////////////////////////////////////////////////////////////////////////////////////////*/

        public static MethodInfo GetWritePrimitive(Type type)
        {
            if (type.IsEnum)
                type = type.GetEnumUnderlyingType();

            MethodInfo writer;

            writer = typeof(Primitives).GetMethod("WritePrimitive", BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null,
                new Type[] { typeof(MemoryStream), type, typeof(Serializer) }, null);

            if (writer != null)
                return writer;

            if (type.IsGenericType)
            {
                var genType = type.GetGenericTypeDefinition();

                var mis = typeof(Primitives).GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .Where(mi => mi.IsGenericMethod && mi.Name == "WritePrimitive");

                foreach (var mi in mis)
                {
                    var p = mi.GetParameters();

                    if (p.Length != 3)
                        continue;

                    if (p[0].ParameterType != typeof(MemoryStream))
                        continue;

                    var paramType = p[1].ParameterType;

                    if (paramType.IsGenericType == false)
                        continue;

                    var genParamType = paramType.GetGenericTypeDefinition();

                    if (genType == genParamType)
                        return mi;
                }
            }

            return null;
        }

        public static MethodInfo GetReadPrimitive(Type type)
        {
            if (type.IsEnum)
                type = type.GetEnumUnderlyingType();

            var reader = typeof(Primitives).GetMethod("ReadPrimitive", BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null,
                new Type[] { typeof(MemoryStream), type.MakeByRefType(), typeof(Serializer) }, null);

            if (reader != null)
                return reader;

            if (type.IsGenericType)
            {
                var genType = type.GetGenericTypeDefinition();

                var mis = typeof(Primitives).GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .Where(mi => mi.IsGenericMethod && mi.Name == "ReadPrimitive");

                foreach (var mi in mis)
                {
                    var p = mi.GetParameters();

                    if (p.Length != 3)
                        continue;

                    if (p[0].ParameterType != typeof(MemoryStream))
                        continue;

                    var paramType = p[1].ParameterType;

                    if (paramType.IsByRef == false)
                        continue;

                    paramType = paramType.GetElementType();

                    if (paramType.IsGenericType == false)
                        continue;

                    var genParamType = paramType.GetGenericTypeDefinition();

                    if (genType == genParamType)
                        return mi;
                }
            }

            return null;
        }

        /*/////////////////////////////////////////////////////////////////////////////////////////*/

        static uint EncodeZigZag32(int n)
        {
            return (uint)((n << 1) ^ (n >> 31));
        }

        static ulong EncodeZigZag64(long n)
        {
            return (ulong)((n << 1) ^ (n >> 63));
        }

        static int DecodeZigZag32(uint n)
        {
            return (int)(n >> 1) ^ -(int)(n & 1);
        }

        static long DecodeZigZag64(ulong n)
        {
            return (long)(n >> 1) ^ -(long)(n & 1);
        }

        static uint ReadVarint32(MemoryStream stream)
        {
            int result = 0;
            int offset = 0;

            for (; offset < 32; offset += 7)
            {
                int b = stream.ReadByte();
                if (b == -1)
                    throw new EndOfStreamException();

                result |= (b & 0x7f) << offset;

                if ((b & 0x80) == 0)
                    return (uint)result;
            }

            throw new InvalidDataException();
        }

        static void WriteVarint32(MemoryStream stream, uint value, Serializer s)
        {
            for (; value >= 0x80u; value >>= 7)
                stream.WriteByte((byte)(value | 0x80u));

            stream.WriteByte((byte)value);
        }

        static ulong ReadVarint64(MemoryStream stream)
        {
            long result = 0;
            int offset = 0;

            for (; offset < 64; offset += 7)
            {
                int b = stream.ReadByte();
                if (b == -1)
                    throw new EndOfStreamException();

                result |= ((long)(b & 0x7f)) << offset;

                if ((b & 0x80) == 0)
                    return (ulong)result;
            }

            throw new InvalidDataException();
        }

        static void WriteVarint64(MemoryStream stream, ulong value, Serializer s)
        {
            for (; value >= 0x80u; value >>= 7)
                stream.WriteByte((byte)(value | 0x80u));

            stream.WriteByte((byte)value);
        }

        /*/////////////////////////////////////////////////////////////////////////////////////////*/

        public static void WritePrimitive(MemoryStream stream, bool value, Serializer s)
        {
            stream.WriteByte(value ? (byte)1 : (byte)0);
        }

        public static void ReadPrimitive(MemoryStream stream, out bool value, Serializer s)
        {
            var b = stream.ReadByte();
            value = b != 0;
        }

        public static void WritePrimitive(MemoryStream stream, byte value, Serializer s)
        {
            stream.WriteByte(value);
        }

        public static void ReadPrimitive(MemoryStream stream, out byte value, Serializer s)
        {
            value = (byte)stream.ReadByte();
        }

        public static void WritePrimitive(MemoryStream stream, sbyte value, Serializer s)
        {
            stream.WriteByte((byte)value);
        }

        public static void ReadPrimitive(MemoryStream stream, out sbyte value, Serializer s)
        {
            value = (sbyte)stream.ReadByte();
        }

        public static void WritePrimitive(MemoryStream stream, char value, Serializer s)
        {
            WriteVarint32(stream, value, s);
        }

        public static void ReadPrimitive(MemoryStream stream, out char value, Serializer s)
        {
            value = (char)ReadVarint32(stream);
        }

        public static void WritePrimitive(MemoryStream stream, ushort value, Serializer s)
        {
            WriteVarint32(stream, value, s);
        }

        public static void ReadPrimitive(MemoryStream stream, out ushort value, Serializer s)
        {
            value = (ushort)ReadVarint32(stream);
        }

        public static void WritePrimitive(MemoryStream stream, short value, Serializer s)
        {
            WriteVarint32(stream, EncodeZigZag32(value), s);
        }

        public static void ReadPrimitive(MemoryStream stream, out short value, Serializer s)
        {
            value = (short)DecodeZigZag32(ReadVarint32(stream));
        }

        public static void WritePrimitive(MemoryStream stream, uint value, Serializer s)
        {
            WriteVarint32(stream, value, s);
        }

        public static void ReadPrimitive(MemoryStream stream, out uint value, Serializer s)
        {
            value = ReadVarint32(stream);
        }

        public static void WritePrimitive(MemoryStream stream, int value, Serializer s)
        {
            WriteVarint32(stream, EncodeZigZag32(value), s);
        }

        public static void ReadPrimitive(MemoryStream stream, out int value, Serializer s)
        {
            value = DecodeZigZag32(ReadVarint32(stream));
        }

        public static void WritePrimitive(MemoryStream stream, ulong value, Serializer s)
        {
            WriteVarint64(stream, value, s);
        }

        public static void ReadPrimitive(MemoryStream stream, out ulong value, Serializer s)
        {
            value = ReadVarint64(stream);
        }

        public static void WritePrimitive(MemoryStream stream, long value, Serializer s)
        {
            WriteVarint64(stream, EncodeZigZag64(value), s);
        }

        public static void ReadPrimitive(MemoryStream stream, out long value, Serializer s)
        {
            value = DecodeZigZag64(ReadVarint64(stream));
        }

        public static unsafe void WritePrimitive(MemoryStream stream, float value, Serializer s)
        {
            uint v = *(uint*)(&value);
            WriteVarint32(stream, v, s);
        }

        public static unsafe void ReadPrimitive(MemoryStream stream, out float value, Serializer s)
        {
            uint v = ReadVarint32(stream);
            value = *(float*)(&v);
        }

        public static unsafe void WritePrimitive(MemoryStream stream, double value, Serializer s)
        {
            ulong v = *(ulong*)(&value);
            WriteVarint64(stream, v, s);
        }

        public static unsafe void ReadPrimitive(MemoryStream stream, out double value, Serializer s)
        {
            ulong v = ReadVarint64(stream);
            value = *(double*)(&v);
        }

        public static unsafe void WritePrimitive(MemoryStream stream, DateTime value, Serializer s)
        {
            long v = value.ToBinary();
            WritePrimitive(stream, v, s);
        }

        public static unsafe void ReadPrimitive(MemoryStream stream, out DateTime value, Serializer s)
        {
            long v;
            ReadPrimitive(stream, out v, s);
            value = DateTime.FromBinary(v);
        }


        public static void WritePrimitive(MemoryStream stream, string value, Serializer s)
        {
            if (value == null)
            {
                WritePrimitive(stream, (uint)0, s);
                return;
            }

            if (s.SerializeStringAsUTF16)
            {
                // Save it as UTF16
                var buf = value.ToCharArray();
                byte[] bff = new byte[buf.Length * 2];
                System.Buffer.BlockCopy(buf, 0, bff, 0, bff.Length);
                WritePrimitive(stream, (uint)bff.Length + 1, s);
                stream.Write(bff, 0, bff.Length);
            }
            else
            {
                // Save it as UTF8
                var buf = mEncoding.GetBytes(value);
                WritePrimitive(stream, (uint)buf.Length + 1, s);
                stream.Write(buf, 0, buf.Length);
            }
        }

        public unsafe static void ReadPrimitive(MemoryStream stream, out string value, Serializer s)
        {
            uint len;
            ReadPrimitive(stream, out len, s);

            if (len == 0)
            {
                value = null;
                return;
            }
            else if (len == 1)
            {
                value = string.Empty;
                return;
            }

            len -= 1;

            var buf = new byte[len];
            stream.Read(buf, 0, (int)len);

            if (s.SerializeStringAsUTF16)
            {
                // Read it as UTF16
                fixed (byte* ac = &buf[0])
                {
                    char* ax = (char*)(ac);
                    value = new string(ax);
                }
            }
            else
            {
                // Read it as UTF8
                value = mEncoding.GetString(buf);
            }
        }



        public static void WritePrimitive(MemoryStream stream, byte[] value, Serializer s)
        {
            if (value == null)
            {
                WritePrimitive(stream, (uint)0, s);
                return;
            }

            WritePrimitive(stream, (uint)value.Length + 1, s);

            stream.Write(value, 0, value.Length);
        }

        static readonly byte[] s_emptyByteArray = new byte[0];

        public static void ReadPrimitive(MemoryStream stream, out byte[] value, Serializer s)
        {
            uint len;
            ReadPrimitive(stream, out len, s);

            if (len == 0)
            {
                value = null;
                return;
            }
            else if (len == 1)
            {
                value = s_emptyByteArray;
                return;
            }

            len -= 1;

            value = new byte[len];
            int l = 0;

            while (l < len)
            {
                int r = stream.Read(value, l, (int)len - l);
                if (r == 0)
                    throw new EndOfStreamException();
                l += r;
            }
        }

        public static void WritePrimitive<TKey, TValue>(MemoryStream stream, Dictionary<TKey, TValue> value, Serializer s)
        {
            var kvpArray = new KeyValuePair<TKey, TValue>[value.Count];

            int i = 0;
            foreach (var kvp in value)
                kvpArray[i++] = kvp;

            s.Serialize(stream, kvpArray);
        }

        public static void ReadPrimitive<TKey, TValue>(MemoryStream stream, out Dictionary<TKey, TValue> value, Serializer s)
        {
            var kvpArray = (KeyValuePair<TKey, TValue>[])s.Deserialize(stream);

            value = new Dictionary<TKey, TValue>(kvpArray.Length);

            foreach (var kvp in kvpArray)
                value.Add(kvp.Key, kvp.Value);
        }
    }
}
