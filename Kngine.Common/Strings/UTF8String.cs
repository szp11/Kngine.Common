using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kngine
{
    [Serializable]
    public struct UTF8String : IEnumerable<char>, IEquatable<UTF8String>
    {
        private readonly byte[] mBytes;

        private readonly int mBytesStartIndex, mBytesLength;

        private static byte[] mCharLength;

        public static readonly UTF8String Empty = new UTF8String(null, 0, 0);

        /*////////////////////////////////////////////////////////////////////////////////////////////////*/

        static UTF8String()
        {
            mCharLength = new Byte[256];
            int i = 0;
            while (/* i>=0x00 && */ i <= 0x7F) mCharLength[i++] = 1;
            while (/* i>=0x80 && */ i <= 0xBF) mCharLength[i++] = 1; // invalid 
            while (/* i>=0xC0 && */ i <= 0xDF) mCharLength[i++] = 2;
            while (/* i>=0xE0 && */ i <= 0xEF) mCharLength[i++] = 3;
            while (/* i>=0xF0 && */ i <= 0xF7) mCharLength[i++] = 1; // 4 but not available in Windows
            while (/* i>=0xF8 && */ i <= 0xFB) mCharLength[i++] = 1; // 5 but not available in Windows
            while (/* i>=0xFC && */ i <= 0xFD) mCharLength[i++] = 1; // 6 but not available in Windows
            mCharLength[0xFE] = 1; // invalid
            mCharLength[0xFF] = 1; // invalid
        }

        public UTF8String(string value)
        {
            mBytesStartIndex = mBytesLength = 0;
            if (value == null || value.Length == 0)
            {
                mBytes = new byte[0];
                return;
            }

            mBytes = Encoding.UTF8.GetBytes(value);
        }

        public UTF8String(byte[] value)
        {
            mBytes = value;
            mBytesStartIndex = 0;
            mBytesLength = mBytes.Length;
        }

        public UTF8String(byte[] value, int index, int length)
        {
            mBytes = value;
            mBytesStartIndex = index;
            mBytesLength = length;
        }

        /*////////////////////////////////////////////////////////////////////////////////////////////////*/

        // NEED TEST
        public static bool Equals(UTF8String a, UTF8String b)
        {
            return object.ReferenceEquals(a, b) || ((object)a != null && (object)b != null &&
                   a.mBytesLength == b.mBytesLength && CompareArrays(a.mBytes, a.mBytesStartIndex, b.mBytes, b.mBytesStartIndex, a.mBytesLength));
        }

        // NEED TEST
        public bool Equals(UTF8String other)
        {
            return Equals(this, other);
        }

        // NEED TEST
        public unsafe override int GetHashCode()
        {
            if (mBytesLength == 0) return 0;
            long remainingBytes = mBytesLength % 8;         // could be: mBytesLength & 7
            long numberOfLoops = mBytesLength >> 4;

            fixed (byte* text1 = &mBytes[mBytesStartIndex])
            {
                byte* chPtr1 = text1;
                long num1 = 0x15051505;
                long num2 = num1;
                long* numPtr1 = (long*)chPtr1;

                if (numberOfLoops > 0)
                {
                    for (long num3 = mBytesLength; num3 > 7; num3 -= 16)
                    {
                        num1 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ numPtr1[0];
                        if (num3 <= 8) break;

                        num2 = (((num2 << 5) + num2) + (num2 >> 0x1b)) ^ numPtr1[1];
                        numPtr1 += 2;
                    }
                }

                switch (remainingBytes)
                {
                    case 7:
                        num1 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ *(short*)(chPtr1);
                        num2 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ *(short*)(chPtr1 + 2);
                        num1 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ *(short*)(chPtr1 + 4);
                        num2 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ *(byte*)(chPtr1 + 6);
                        break;
                    case 6:
                        //num1 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ *(int*)(chPtr1);
                        num1 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ *(short*)(chPtr1);
                        num2 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ *(short*)(chPtr1 + 2);
                        num1 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ *(short*)(chPtr1 + 4);
                        break;
                    case 5:
                        num1 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ *(short*)(chPtr1);
                        num2 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ *(short*)(chPtr1 + 2);
                        num1 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ *(byte*)(chPtr1 + 4);
                        break;
                    case 4:
                        num1 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ *(int*)(chPtr1);
                        break;
                    case 3:
                        num1 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ *(short*)(chPtr1);
                        num2 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ *(byte*)(chPtr1 + 1);
                        break;
                    case 2:
                        num1 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ *(short*)(chPtr1);
                        break;
                    case 1:
                        num1 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ *(byte*)(chPtr1);
                        break;
                    default:
                        break;
                }

                return (int)((num1 + (num2 * 0x5d588b65)) % int.MaxValue);
            }

        }

        // NEED TEST
        public override bool Equals(object obj)
        {
            if (obj is UTF8String) 
                return Equals(this, (UTF8String)obj);
            else if (obj is byte[])
            {
                return object.ReferenceEquals(this, obj) || ((object)this != null && obj != null &&
                       CompareArrays(mBytes, mBytesStartIndex, obj as byte[], 0, mBytesLength));
            }
            return false;
        }

        // NEED TEST
        public override string ToString()
        {
            return System.Text.Encoding.UTF8.GetString(mBytes, mBytesStartIndex, mBytesLength);
        }

        /*////////////////////////////////////////////////////////////////////////////////////////////////*/

        public static implicit operator UTF8String(byte[] value)
        {
            return new UTF8String(value);
        }

        // NEED TEST
        public static UTF8String operator +(UTF8String a, UTF8String b)
        {
            int alength = a.mBytesLength;
            int blength = b.mBytesLength;
            var newBytes = new Byte[alength + blength];

            Array.Copy(a.mBytes, a.mBytesStartIndex, newBytes, 0, alength);
            Array.Copy(b.mBytes, b.mBytesStartIndex, newBytes, alength, blength);
            return new UTF8String(newBytes);
        }

        public static bool operator ==(UTF8String a, UTF8String b)
        {
            return UTF8String.Equals(a, b);
        }

        public static bool operator !=(UTF8String a, UTF8String b)
        {
            return !UTF8String.Equals(a, b);
        }


        private static bool OldCompareArrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            
            int len = a.Length;
            for (int i = 0; i < len; i++)
            {
                if (a[i] != b[i]) return false;
            }

            return true;
        }

        private static bool CompareArrays(byte[] a, int aStartIndex, byte[] b, int bStartIndex, int len)
        {
            len = len + aStartIndex;
            for (; aStartIndex < len; ++aStartIndex, ++bStartIndex)
            {
                if (a[aStartIndex] != b[bStartIndex]) return false;
            }

            return true;
        }

        /*////////////////////////////////////////////////////////////////////////////////////////////////*/

        // NEED TEST
        public int Length
        {
            get
            {
                int result = 0;
                int index = mBytesStartIndex;
                int len = index + mBytesLength;

                while (index < len)
                {
                    index += mCharLength[mBytes[index]];
                    ++result;
                }

                return result;
            }
        }

        /*////////////////////////////////////////////////////////////////////////////////////////////////*/

        // NEED TEST
        public UTF8String SubString(int startIndex, int length)
        {
            if (startIndex < 0) throw new ArgumentOutOfRangeException("startIndex");
            var startmBytesIndex = GetBytesIndex(startIndex, mBytesStartIndex);
            if (startmBytesIndex < 0) throw new ArgumentOutOfRangeException("startIndex");

            if (length == 0) return UTF8String.Empty;
            if (length < 0) throw new ArgumentOutOfRangeException("length");

            var endmBytesIndex = GetBytesIndex(length, startmBytesIndex);
            if (endmBytesIndex < 0) throw new ArgumentOutOfRangeException("length");

            if (startIndex == 0 && length == mBytes.Length) return this;

            var newBytes = new Byte[endmBytesIndex - startmBytesIndex];
            Array.Copy(mBytes, startmBytesIndex, newBytes, 0, endmBytesIndex - startmBytesIndex);
            return new UTF8String(newBytes);
        }

        // NEED TEST
        public UTF8String SubString(int startIndex)
        {
            if (startIndex == 0) return this;

            if (startIndex < 0) throw new ArgumentOutOfRangeException("startIndex");
            var startmBytesIndex = GetBytesIndex(startIndex, mBytesStartIndex);
            if (startmBytesIndex < 0) throw new ArgumentOutOfRangeException("startIndex");

            var newBytes = new Byte[mBytes.Length - startmBytesIndex];
            Array.Copy(mBytes, startmBytesIndex, newBytes, 0, mBytes.Length - startmBytesIndex);
            return new UTF8String(newBytes);
        }

        // NEED TEST
        public IEnumerator<char> GetEnumerator()
        {
            return new Utf8StringEnumerator(this);
        }


        private int GetBytesIndex(int charCount, int mbytesIndex = 0)
        {
            if (charCount == 0) return mbytesIndex;
            int len = mBytes.Length;
            while (mbytesIndex < len)
            {
                mbytesIndex += mCharLength[mBytes[mbytesIndex]];
                charCount--;
                if (charCount == 0) return mbytesIndex;
            }
            return -1;
        }

        /*////////////////////////////////////////////////////////////////////////////////////////////////*/

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Utf8StringEnumerator(this);
        }

        class Utf8StringEnumerator : IEnumerator<char>
        {
            private Byte[] mBytes;
            private char mCurChar;

            private int mIndex;
            private int mBytesLength;
            private int mBytesStartIndex;
            

            public char Current
            {
                get
                {
                    return mCurChar;
                }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return mCurChar; }
            }

            internal Utf8StringEnumerator(UTF8String source)
            {
                mBytes = source.mBytes;
                mBytesLength = source.mBytesLength; 
                mBytesStartIndex = source.mBytesStartIndex;

                Reset();
            }

            public bool MoveNext()
            {
                if (mIndex < mBytesLength)
                {
                    int curCharLength = UTF8String.mCharLength[mBytes[mIndex]];
                    if (mIndex + curCharLength < mBytesLength)
                    {
                        mCurChar = System.Text.Encoding.UTF8.GetString(mBytes, mIndex, curCharLength)[0];
                        mIndex += curCharLength;
                        return true;
                    }
                }
                return false;
            }

            public void Reset()
            {
                this.mCurChar = '\0';
                this.mIndex = mBytesStartIndex;
            }

            public void Dispose()
            {
            }

        }
    }
}
