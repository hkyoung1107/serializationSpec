using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;

namespace java.serialize
{
    /// <summary>
    /// The purpose of this class is different from ByteBuffer(1).
    /// Previous ByteBuffer(1) is dynamic and self contained container 
    /// whereas ByteBuffer2 is based on shallow-copied buffer.
    /// 
    /// So, the size of this class is fixed once created and the contents of this buffer 
    /// will be changed as the contents of base byte[] change.
    /// 
    /// </summary>
    public class ByteBuffer2
    {
        protected int begin, count;
        protected int limit;
        protected byte[] data;

        // private static readonly object locked = new object();
        // private static SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();

        public byte[] Data { get { return data; } }

        public ByteBuffer2(): this(null, 0, 0)
        {
        }

        public ByteBuffer2(byte[] buffer): this(buffer, 0, buffer.Length)
        {
        }

        public ByteBuffer2(byte[] buffer, int offset, int count)
        {
            this.begin  = offset;
            this.Offset = offset;
            this.limit  = offset + count;
            this.data   = buffer;
            this.count  = count;
        }

        public int Size         { get { return this.count; } }

        public int RemainedSize { get { return this.limit - this.Offset; } }

        public int Offset       { get; set; }

        public int End          { get { return limit; } }

        public bool Empty()
        {
            return this.Offset >= this.limit;
        }

        public int Find(string s, int startAt = 0)
        {
            int result = -1;
            var bytes = Encoding.Default.GetBytes(s);
            int start = Math.Max(this.begin, startAt);
            for (int i=start; i<limit; i++)
            {
                if (this.CompareRange(i, bytes.Length, bytes))
                {
                    result = i;
                    break;
                }
            }

            return result;
        }

        public Byte Peek()
        {
            return this.data[this.Offset];
        }

        public ByteBuffer2 Flip()
        {
            this.Offset = this.begin;

            return this;
        }

        public ByteBuffer2 Reset()
        {
            this.Offset = this.begin;

            return this;
        }

        public bool HasRemaining()
        {
            return this.Offset < this.limit;
        }

        public bool CanRead(int toRead)
        {
            return toRead > 0 && toRead <= RemainedSize;
        }

        #region string
        // defualt Deleimeter : 0x00
        private int getStringLength(byte delimiter = byte.MinValue)
        {
            for (int index = 0; this.Offset + index < this.limit; index++)
            {
                if (delimiter != byte.MinValue && this.data[this.Offset + index] == delimiter)
                {
                    return index;
                }
              
                if (this.data[this.Offset + index] == 0)
                {
                    return index;
                }
            }

            return (delimiter == byte.MinValue)
                ? this.limit - this.Offset
                : -1;
        }

        # region default
        public string GetDefaultString(byte delimiter)
        {
            var index = getStringLength(delimiter);
            return (index == -1) ? "" : GetDefaultString(index);
        }

        public string GetDefaultString()
        {
            var index = getStringLength();
            return (index == -1) ? "" : GetDefaultString(index);
        }

        public string GetDefaultString(int size, int at = -1, byte delimiter = byte.MinValue)
        {
            var pos = at >= 0 ? at : this.Offset;
            if (pos + size > this.data.Length)
            {
                size = this.data.Length - pos;
                if (size <= 0)
                    return "";
            }

            var here = advance(at, size);
            return Encoding.Default.GetString(this.data, here, size);
        }
        #endregion

        # region Ascii
        public string GetAscii(byte delimiter)
        {
            // Python에서 사용할 때는 아래와 같이 사용합니다.
            // from System import Byte
            // file.Read(0, 16).GetAscii(Byte(255))
            
            var index = getStringLength(delimiter);
            return (index  == -1) ? "" : GetAscii(index);
        }

        public string GetAscii()
        {
            var len = getStringLength();

            if (len == 0)
            {
                this.Offset += 1;
                return "";
            }

            if (len < 0)
                return "";

            var ascii = Encoding.ASCII.GetString(data, Offset, len);
            this.Offset += ascii.Length + 1;

            return ascii;
        }

        public string GetAscii(int size, int at = -1)
        {
            var pos = at >= 0 ? at : this.Offset;
            if (pos + size > this.data.Length)
            {
                size = this.data.Length - pos;
                if (size <= 0)
                    return "";
            }

            var here = advance(at, size);
            return Encoding.ASCII.GetString(this.data, here, size);
        }
        #endregion

        #region UTF8

        public string GetUTF8(int size, int at = -1)
        {
            var pos = at >= 0 ? at : this.Offset;
            if (pos + size > this.data.Length)
            {
                size = this.data.Length - pos;
                if (size <= 0)
                    return "";
            }

            var here = advance(at, size);
            return Encoding.UTF8.GetString(this.data, here, size);
        }

        #endregion

        private static string[] HexTbl = Enumerable.Range(0, 256).Select(v => v.ToString("X2")).ToArray();

        public string ToHex()
        {
            var hexRepr = new StringBuilder((this.limit - this.Offset) * 2);
            for (int i = this.Offset; i < this.limit; i++)
                hexRepr.Append(HexTbl[this.data[i]]);

            return hexRepr.ToString();
        }

        public string ToHexSwapNibble(bool hasPadding = false)
        {
            string hexcode = ToHex();
                       
            string str = "";
            for(int i = 0; i < hexcode.Length; i +=2 )
            {
                str += hexcode[i + 1];
                str += hexcode[i];
            }

            if (!string.IsNullOrEmpty(str) && hasPadding)
            {
                while(str[str.Length -1] == 'F')
                {
                    str = str.Substring(0, str.Length - 1);
                }
                
            }

            return str;
        }
        
        public string ToSMSPDU()
        {
            string str = "";
            int sl = 0;
            byte mask = 0x7F;
            int r = 0;
            for (int i = 0; i < this.data.Length; i++)
            {
                var b = this.data[i] & mask;
                var c = (char)((b << sl) + r);
                str += c;

                r = this.data[i] & (0xFF - mask);
                r >>= 7 - sl;

                mask >>= 1;
                sl++;

                if (mask == 1)
                {
                    str += (char)(this.data[i] >> 1);

                    mask = 0x7F;
                    sl = 0;
                }
            }

            return str;
        }

        #region Unicode
        public string GetUnicode()
        {
            int pos = data.Length;
            for (int i=this.Offset; i<data.Length-1; i+=2)
            {
                if (data[i] == 0 && data[i+1] == 0)
                {
                    pos = i;
                    break;
                }
            }

            //if (pos == data.Length)
            //    throw new Exception("ByteBuffer2.GetUnicde: Unicode not found");

            int sz = pos - this.Offset;
            var unicode = Encoding.Unicode.GetString(data, this.Offset, sz);
            this.Offset = pos + 2;

            return unicode;
        }

		public string GetUnicodeBE()
		{
			int pos = data.Length;
			for (int i = this.Offset; i < data.Length - 1; i += 2)
			{
				if (data[i] == 0 && data[i + 1] == 0)
				{
					pos = i;
					break;
				}
			}

			int sz = pos - this.Offset;
			var unicode = Encoding.BigEndianUnicode.GetString(data, this.Offset, sz);
			this.Offset = pos + 2;

			return unicode;
		}


		public string GetUnicode(int size)
        {
            if (this.Offset + size*2 > this.data.Length)
                throw new IndexOutOfRangeException(
                    "ByteBuffer2.GetUnicde: The size exceeds current buffer range");

            var res = Encoding.Unicode.GetString(this.data, this.Offset, size*2);
            this.Offset += size*2;

            return res;
        }
        #endregion

        #endregion

        #region numeric

        public long GetIntLE(int size)
        {
            if (size <= 0 || size > 8)
                throw new Exception("Invalid integer size");

            long res = -1;
            switch (size)
            {
                case 1: res = (long)this.GetSByte(); break;
                case 2: res = (long)this.GetInt16LE(); break;
                case 3: res = (long)this.GetInt24LE(); break;
                case 4: res = (long)this.GetInt32LE(); break;
                case 5: res = (long)this.GetInt40LE(); break;
                case 6: res = (long)this.GetInt48LE(); break;
                case 7: res = (long)this.GetInt56LE(); break;
                case 8: res = (long)this.GetInt64LE(); break;
            }

            return res;
        }

        public sbyte GetInt8(int at = -1)
        {
            if (this.Offset + 1 > this.data.Length)
                throw new IndexOutOfRangeException("The (index, size) pair is not valid");

            var here = advance(at, 1);
            return (sbyte)this.data[here];
        }

        public UInt16 GetUInt16BE(int at = -1)
        {
            var here = advance(at, 2);
            var r = (UInt16) this.data[here]; r <<= 8;
            r |= this.data[here+1];

            return (UInt16)r;
        }

        public Int16 GetInt16BE(int at = -1)
        {
            return (Int16)this.GetUInt16BE(at);
        }

        public UInt16 GetUInt16LE(int at = -1)
        {
            var here = advance(at, 2);

            var r = (UInt16)this.data[here + 1]; r <<= 8;
            r |= this.data[here];

            return r;
        }

        public Int16 GetInt16LE(int at = -1)
        {
            return (Int16)this.GetUInt16LE(at);
        }

        public UInt32 GetUInt24BE(int at = -1)
        {
            var here = advance(at, 3);

            UInt32 r = (UInt32)this.data[here]; r <<= 8;
            r |= this.data[here + 1]; r <<= 8;
            r |= this.data[here + 2]; 

            return r;
        }

        public Int32 GetInt24BE(int at = -1)
        {
            var here = advance(at, dist: 3);

            var first = this.data[here]; 
            var l = (Int32) leadingByte(first);
            var r = (l << 8) | first; r <<= 8;         // complete first 2
            r |= this.data[here + 1]; r <<= 8;         // complete first 3
            r |= this.data[here + 2];                  // complete first 4

            return (Int32)r;
        }

        public UInt32 GetUInt24LE(int at = -1)
        {
            var here = advance(at, 3);

            var r = (UInt32)this.data[here+2]; r <<= 8;
            r |= this.data[here+1]; r <<= 8;
            r |= this.data[here];

            return r;
        }

        public Int32 GetInt24LE(int at = -1)
        {
            var here = advance(at, dist: 3);

            var first = this.data[here+2]; 
            var l = (Int32) leadingByte(first);
            var r = (l << 8) | first; r <<= 8;         // complete first 2
            r |= this.data[here + 1]; r <<= 8;         // complete first 3
            r |= this.data[here];                      // complete first 4

            return (Int32)r;
        }

        public UInt32 GetUInt32BE(int at = -1)
        {
            var here = advance(at, 4);

            var r = (UInt32)this.data[here]; r <<= 8;
            r |= this.data[here + 1]; r <<= 8;
            r |= this.data[here + 2]; r <<= 8;
            r |= this.data[here + 3];

            return r;
        }

        public Int32 GetInt32BE(int at = -1)
        {
            return (Int32)this.GetUInt32BE(at);
        }

        public UInt32 GetUInt32LE(int at = -1)
        {
            var here = advance(at, 4);

            var r = (UInt32)this.data[here+3]; r <<= 8;
            r |= this.data[here + 2]; r <<= 8;
            r |= this.data[here + 1]; r <<= 8;
            r |= this.data[here];

            return r;
        }

        public Int32 GetInt32LE(int at = -1)
        {
            return (Int32) this.GetUInt32LE(at);
        }

        public UInt64 GetUInt40BE(int at = -1)
        {
            var here = advance(at, 5);

            var r = (UInt64)this.data[here]; r <<= 8;
            for (int i=1; i<4; i++) { r |= this.data[here + i]; r <<= 8; }
            r |= this.data[here + 4]; 

            return r;
        }

        public Int64 GetInt40BE(int at = -1)
        {
            var here = advance(at, dist: 5);

            var first = this.data[here]; 
            var l = (Int64) leadingByte(first);
            var r = (l << 24) | (l << 16) | (l << 8) | first; r <<= 8; // completed first 4

            for (int i=1; i<=3; i++) { r |= this.data[here + i]; r <<= 8; } // 5, 6, 7 
            r |= this.data[here + 4];                                       // complete first 8

            return r;
        }

        public UInt64 GetUInt40LE(int at = -1)
        {
            var here = advance(at, 5);

            var r = (UInt64)this.data[here+4]; r <<= 8;
            for (int i=3; i>0; i--) { r |= this.data[here + i]; r <<= 8; }
            r |= this.data[here];

            return r;
        }

        public Int64 GetInt40LE(int at = -1)
        {
            var here = advance(at, dist: 5);

            var first = this.data[here+4]; 
            var l = (Int64) leadingByte(first);
            var r = (l << 24) | (l << 16) | (l << 8) | first; r <<= 8;
                                                 // completed first 4
            for (int i=3; i>0; i--) { r |= this.data[here + i]; r <<= 8; } // 4, 5, 6, 7
            r |= this.data[here + 0];            // complete first 8

            return r;
        }

        public UInt64 GetUInt48BE(int at = -1)
        { 
            var here = advance(at, 6);

            var r = (UInt64)this.data[here]; r <<= 8;
            for (int i=1; i<=4; i++) { r |= this.data[here + i]; r <<= 8; }
            r |= this.data[here + 5]; 

            return r;
        }

        public Int64 GetInt48BE(int at = -1)
        { 
            var here = advance(at, dist: 6);

            var first = this.data[here]; 
            var l = (Int64) leadingByte(first);
            var r = (l << 16) | (l << 8) | first; r <<= 8; // completed first 3

            for (int i=1; i<=4; i++) { r |= this.data[here + i]; r <<= 8; } // 4, 5, 6, 7
            r |= this.data[here + 5];                                       // complete first 8

            return r;
        }

        public UInt64 GetUInt48LE(int at = -1)
        { 
            var here = advance(at, 6);

            var r = (UInt64)this.data[here+5]; r <<= 8;
            for (int i=4; i>0; i--) { r |= this.data[here + i]; r <<= 8; }
            r |= this.data[here];

            return r;
        }

        public Int64 GetInt48LE(int at = -1)
        { 
            var here = advance(at, dist: 6);

            var first = this.data[here+5]; 
            var l = (Int64) leadingByte(first);
            var r = (l << 16) | (l << 8) | first; r <<= 8;                 // completed first 3
            for (int i=4; i>0; i--) { r |= this.data[here + i]; r <<= 8; } // 4, 5, 6, 7
            r |= this.data[here + 0];                                      // complete first 8

            return r;
        }

        public UInt64 GetUInt56BE(int at = -1)
        { 
            var here = advance(at, 7);

            var r = (UInt64)this.data[here]; r <<= 8;
            for (int i=1; i<=5; i++) { r |= this.data[here + i]; r <<= 8; }
            r |= this.data[here + 6]; 

            return r;
        }

        public Int64 GetInt56BE(int at = -1)
        { 
            var here = advance(at, dist: 7);

            var first = this.data[here]; 
            var l = (Int64) leadingByte(first);
            var r = (l << 8) | first; r <<= 8; // completed first 2

            for (int i=1; i<=5; i++) { r |= this.data[here + i]; r <<= 8; } // 3, 4, 5, 6, 7
            r |= this.data[here + 6];                                       // complete first 8

            return r;
        }

        public UInt64 GetUInt56LE(int at = -1)
        { 
            var here = advance(at, 7);

            var r = (UInt64)this.data[here+6]; r <<= 8;
            for (int i=5; i>0; i--) { r |= this.data[here + i]; r <<= 8; }
            r |= this.data[here];

            return r;
        }

        public Int64 GetInt56LE(int at = -1)
        { 
            var here = advance(at, dist: 7);

            var first = this.data[here+6]; 
            var l = (Int64) leadingByte(first);
            var r = (l << 8) | first; r <<= 8;                             // completed first 2
            for (int i=5; i>0; i--) { r |= this.data[here + i]; r <<= 8; } // 3, 4, 5, 6, 7
            r |= this.data[here + 0];                                      // complete first 8

            return r;
        }

        public UInt64 GetUInt64BE(int at = -1)
        { 
            var here = advance(at, 8);

            var r = (UInt64)this.data[here]; r <<= 8;
            for (int i=1; i<7; i++) { r |= this.data[here + i]; r <<= 8; }
            r |= this.data[here + 7]; 

            return r;
        }

        public Int64 GetInt64BE(int at = -1)
        { 
            return (Int64)this.GetUInt64BE(at);
        }

        public UInt64 GetUInt64LE(int at = -1)
        { 
            var here = advance(at, 8);

            var r = (UInt64)this.data[here+7]; r <<= 8;
            for (int i=6; i>0; i--) { r |= this.data[here + i]; r <<= 8; }
            r |= this.data[here];

            return r;
        }

        public Int64 GetInt64LE(int at = -1)
        { 
            return (Int64)this.GetUInt64LE(at);
        }

        public double GetDouble(int at = -1)
        {
            var here = advance(at, 8);

            var arr = new byte[8];
            Buffer.BlockCopy(this.data, here, arr, 0, 8);
            Array.Reverse(arr);
            return BitConverter.ToDouble(arr, 0);
        }

        public double GetDoubleLE(int at = -1)
        {
            var here = advance(at, 8);

            var arr = new byte[8];
            Buffer.BlockCopy(this.data, here, arr, 0, 8);

            return BitConverter.ToDouble(arr, 0);
        }

        public UInt64 GetVarInt()
        {
            int size;

            return GetVarInt(out size);
        }

        // NOTE
        // to prevent exception happens replicated some code
        //
        public UInt64 GetVarInt(out int size)
        {
            const UInt32 SLOT_2_0 = 0x001fc07f;
            const UInt32 SLOT_4_2_0 = 0xf01fc07f;

            size = 0;

            if (this.Offset == this.limit)
            {
                size = -1;
                return 0;
            }

            var a = (UInt32)this.data[this.Offset++];
            if ((a & 0x80) == 0)
            {
                size = 1;
                return a;
            }

            if (this.Offset == this.limit)
            {
                size = -1;
                return 0;
            }

            var b = (UInt32)this.data[this.Offset++];
            if ((b & 0x80u) == 0)
            {
                size = 2;
                a &= 0x7f;
                a = a << 7;
                a = a | b;

                return a;
            }

            if (this.Offset == this.limit)
            {
                size = -1;
                return 0;
            }

            a = a << 14;
            a = a | this.data[this.Offset++];

            if ((a & 0x80u) == 0)
            {
                size = 3;
                a &= SLOT_2_0;
                b &= 0x7f;
                b = b << 7;
                a = a | b;

                return a;
            }

            if (this.Offset == this.limit)
            {
                size = -1;
                return 0;
            }

            a = a & SLOT_2_0;
            b = b << 14;
            b = b | this.data[this.Offset++];

            /* b: p1<<14 | p3 (unmasked) */
            if ((b & 0x80u) == 0)
            {
                size = 4;
                b = b & SLOT_2_0;
                a = a << 7;
                a = a | b;
                return a;
            }

            b &= SLOT_2_0;
            UInt32 s = a;

            if (this.Offset == this.limit)
            {
                size = -1;
                return 0;
            }

            a = a << 14;
            a = a | this.data[this.Offset++];

            if ((a & 0x80u) == 0)
            {
                size = 5;
                b = b << 7;
                a = a | b;
                s = s >> 18;

                return (UInt64)s << 32 | a;
            }

            if (this.Offset == this.limit)
            {
                size = -1;
                return 0;
            }

            s = s << 7;
            s = s | b;

            b = b << 14;
            b = b | this.data[this.Offset++];
            if ((b & 0x80) == 0)
            {
                size = 6;
                a &= SLOT_2_0;
                a = a << 7;
                a = a | b;
                s = s >> 18;

                return (UInt64)s << 32 | a;
            }

            if (this.Offset == this.limit)
            {
                size = -1;
                return 0;
            }

            a = a << 14;
            a = a | this.data[this.Offset++];
            if ((a & 0x80) == 0)
            {
                size = 7;
                a &= SLOT_4_2_0;
                b &= SLOT_2_0;
                b = b << 7;
                a |= b;
                s = s >> 11;

                return (UInt64)s << 32 | a;
            }

            if (this.Offset == this.limit)
            {
                size = -1;
                return 0;
            }

            /* CSE2 from below */
            a &= SLOT_2_0;
            b = b << 14;
            b = b | this.data[this.Offset++];
            if ((b & 0x80) == 0)
            {
                size = 8;
                b &= SLOT_4_2_0;
                a = a << 7;
                a |= b;
                s = s >> 4;

                return (UInt64)s << 32 | a;
            }

            if (this.Offset == this.limit)
            {
                size = -1;
                return 0;
            }

            size = 9;
            a = a << 15;
            // a = a | this.data[this.Offset++];
            a = a | this.data[this.Offset];

            b &= SLOT_2_0;
            b = b << 8;
            a = a | b;

            s = s << 4;
            b = this.data[this.Offset - 4];
            b = b & 0x7f;
            b = b >> 3;
            s = s | b;

            this.Offset++;

            return (UInt64)s << 32 | a;
        }

        // Note
        // 3개 이상 항목 수정 및 확인 by nalgebra
        public UInt64 GetVarIntLE(out int size)
        {
            const UInt32 SLOT_2_0 = 0x001fc07f;
            const UInt32 SLOT_4_2_0 = 0xf01fc07f;

            size = 0;

            if (this.Offset == this.limit)
            {
                size = -1;
                return 0;
            }

            var a = (UInt32)this.data[this.Offset++];
            if ((a & 0x80) == 0)
            {
                size = 1;
                return a;
            }

            if (this.Offset == this.limit)
            {
                size = -1;
                return 0;
            }

            var b = (UInt32)this.data[this.Offset++];
            if ((b & 0x80u) == 0)
            {
                size = 2;
                a &= 0x7f;
                b = b << 7;
                a = a | b;

                return a;
            }


            if (this.Offset == this.limit)
            {
                size = -1;
                return 0;
            }

            a = a << 14;
            a = a | this.data[this.Offset++];

            if ((a & 0x80u) == 0)
            {
                size = 3;
                a &= SLOT_2_0;
                b &= 0x7f;
                b = b << 7;
                a = a | b;

                return a;
            }

            if (this.Offset == this.limit)
            {
                size = -1;
                return 0;
            }

            a = a & SLOT_2_0;
            b = b << 14;
            b = b | this.data[this.Offset++];

            /* b: p1<<14 | p3 (unmasked) */
            if ((b & 0x80u) == 0)
            {
                size = 4;
                b = b & SLOT_2_0;
                a = a << 7;
                a = a | b;
                return a;
            }

            b &= SLOT_2_0;
            UInt32 s = a;

            if (this.Offset == this.limit)
            {
                size = -1;
                return 0;
            }

            a = a << 14;
            a = a | this.data[this.Offset++];

            if ((a & 0x80u) == 0)
            {
                size = 5;
                b = b << 7;
                a = a | b;
                s = s >> 18;

                return (UInt64)s << 32 | a;
            }

            if (this.Offset == this.limit)
            {
                size = -1;
                return 0;
            }

            s = s << 7;
            s = s | b;

            b = b << 14;
            b = b | this.data[this.Offset++];
            if ((b & 0x80) == 0)
            {
                size = 6;
                a &= SLOT_2_0;
                a = a << 7;
                a = a | b;
                s = s >> 18;

                return (UInt64)s << 32 | a;
            }

            if (this.Offset == this.limit)
            {
                size = -1;
                return 0;
            }

            a = a << 14;
            a = a | this.data[this.Offset++];
            if ((a & 0x80) == 0)
            {
                size = 7;
                a &= SLOT_4_2_0;
                b &= SLOT_2_0;
                b = b << 7;
                a |= b;
                s = s >> 11;

                return (UInt64)s << 32 | a;
            }

            if (this.Offset == this.limit)
            {
                size = -1;
                return 0;
            }

            /* CSE2 from below */
            a &= SLOT_2_0;
            b = b << 14;
            b = b | this.data[this.Offset++];
            if ((b & 0x80) == 0)
            {
                size = 8;
                b &= SLOT_4_2_0;
                a = a << 7;
                a |= b;
                s = s >> 4;

                return (UInt64)s << 32 | a;
            }

            if (this.Offset == this.limit)
            {
                size = -1;
                return 0;
            }

            size = 9;
            a = a << 15;
            // a = a | this.data[this.Offset++];
            a = a | this.data[this.Offset];

            b &= SLOT_2_0;
            b = b << 8;
            a = a | b;

            s = s << 4;
            b = this.data[this.Offset - 4];
            b = b & 0x7f;
            b = b >> 3;
            s = s | b;

            this.Offset++;

            return (UInt64)s << 32 | a;
        }
        #endregion

        #region Phonenumber
        public string ToPNByte()
        {
            string str = "";
            for (int i = this.Offset; i < this.limit; i++ )
            {
                var b = this.data[i];
                if (b > 0xC ) 
                    return str;

                if (b >= 0xA)
                    str += (char)(b + 0x30 - 0xA);
                else
                    str += (char)(b + 0x30);
            }

            return str;
        }

        public string ToPN4Bit()
        {
            string str = "";
            for (int i = this.Offset; i < this.limit; i++)
            {
                var b = this.data[i];
                var b1 = b & 0xF;
                if (b1 == 0xF) 
                    return str;

                str += (b1 < 0x0A) ? (char)(b1 + 0x30) : replaceCharForPN4Bit(b1);

                var b2 = (b & 0xF0) >> 4;
                if (b2 == 0xF) 
                    return str;

                str += (b2 < 0x0A) ? (char)(b2 + 0x30) : replaceCharForPN4Bit(b2);
            }

            return str;
        }

        private char replaceCharForPN4Bit(int b)
        {
            var val = char.MinValue;
            if (b == 0x0A) 
            { 
                val = '*'; 
            }
            else if (b == 0x0B) 
            { 
                val = '#';
            }

            // 0x0C : DTMF(dual tone multi frequency)
			// 0x0D : 'Wild'Value, MMI
			// 0x0E : RFU
            return val;
        }

        public string ToPNBitShift()
        {
            if ((this.limit - this.Offset) < 2)
                return "";

            string str = "";
            var len = ((this.data[this.Offset] & 0x7) << 1) + ((this.data[this.Offset+1] & 0x80) >> 7);

            int rd = -1;
            for (int i = this.Offset + 1; i < this.limit && len > 0; i++)
            {
                if (rd >= 0)
                {
                    var d1 = (rd << 1) + ((this.data[i] & 0x80) >> 7);
                    if (d1 >= 0xA)
                        str += (char)(d1 + 0x30 - 0xA);
                    else
                        str += (char)(d1 + 0x30);

                    len--;
                }

                var d2 = (this.data[i] & 0x78) >> 3;
                if (d2 >= 0xA)
                    str += (char)(d2 + 0x30 - 0xA);
                else
                    str += (char)(d2 + 0x30);

                len--;

                rd = this.data[i] & 0x7;
            }

            return str;
        }

        public string ToPN4BitUSIM()
        {
            string str = "";

            var typeOfNumber = (this.data[this.Offset] & 0x70) >> 4;
            switch (typeOfNumber)
            {
                case 0: // unknown
                    break;
                case 1: // international phone number
                    str = "+";
                    break;
                case 2: // national number
                    break;
                case 3: // network specific number
                case 4: // Subscriber number
                case 5: // Alphanumeric
                case 6: // Abbreviated number
                case 7: // reserved
                    break;
            }

            var numberingPlan = this.data[this.Offset] & 0x0F;
            switch (numberingPlan)
            { 
                case 0: // unknown
                    break;
                case 1: // isdn / telephone
                    {
                        var nBuf = new byte[this.data.Length - 1];
                        this.Offset += 1;
                        str += ToPN4Bit();
                        return str;
                    }
                case 3: // data
                case 4: // telex
                case 8: // national
                case 10: // ERMES
                    //assert(0);
                    break;
                default: // reserved
                    break;
            }

            return str;
        }
        #endregion

        #region DateTime
        public DateTime GetDateTime12B()
        {
            var res = DateTime.MinValue;
            var len = this.limit - this.Offset;
            if (len < 12)
                return res;

            while (res == DateTime.MinValue)
            {
                int year = GetInt16LE(this.Offset);
                if (year < 0) { break; }

                int mon = GetInt16LE(this.Offset + 2);
                if (mon < 1 || mon > 12) { break; }

                int day = GetInt16LE(this.Offset + 4);
                if (day < 1 || day > 31) { break; }

                int hour = GetInt16LE(this.Offset + 6);
                if (hour < 0 || hour > 24) { break; }

                int min = GetInt16LE(this.Offset + 8);
                if (min < 0 || min > 60) { break; }

                int sec = GetInt16LE(this.Offset + 10);
                if (sec < 0 || sec > 60) { break; }

                res = new DateTime(year, mon, day, hour, min, sec);
                break;
            }

            return res;
        }
		public DateTime GetDateTime10B()
		{
			var res = DateTime.MinValue;
			var len = this.limit - this.Offset;
			if (len < 10)
				return res;

			while (res == DateTime.MinValue)
			{
				int year = GetInt16LE(this.Offset);
				if (year < 0) { break; }

				int mon = GetInt16LE(this.Offset + 2);
				if (mon < 1 || mon > 12) { break; }

				int day = GetInt16LE(this.Offset + 4);
				if (day < 1 || day > 31) { break; }

				int hour = GetInt16LE(this.Offset + 6);
				if (hour < 0 || hour > 24) { break; }

				int min = GetInt16LE(this.Offset + 8);
				if (min < 0 || min > 60) { break; }

				int sec = 0;

				res = new DateTime(year, mon, day, hour, min, sec);
				break;
			}

			return res;
		}

		public DateTime GetDateTime7B()
        {
            var res = DateTime.MinValue;
            var len = this.limit - this.Offset;
            if (len < 7)
                return res;

            while (res == DateTime.MinValue)
            {
                int year = GetInt16LE(this.Offset);
                if (year < 0) { break; }
                
                int mon = GetByte(this.Offset + 2);
                if ( mon < 1 || mon > 12 ) { break; }

                int day = GetByte(this.Offset + 3);
                if (day < 1 || day > 31) { break; }

                int hour = GetByte(this.Offset + 4);
                if (hour < 0 || hour > 24) { break; }

                int min = GetByte(this.Offset + 5);
                if (min < 0 || min > 60) { break; }
                
                int sec = GetByte(this.Offset + 6);
                if (sec < 0 || sec > 60) { break; }

                res = new DateTime(year, mon, day, hour, min, sec);
                break;
            }

            return res;
        }

        public DateTime GetDateTime7B_RevYear()
        {
            var res = DateTime.MinValue;
            var len = this.limit - this.Offset;
            if (len < 7)
                return res;

            while (res == DateTime.MinValue)
            {
                int year = GetUInt16BE(this.Offset);
                if (year < 0) { break; }

                int mon = GetByte(this.Offset + 2);
                if (mon < 1 || mon > 12) { break; }

                int day = GetByte(this.Offset + 3);
                if (day < 1 || day > 31) { break; }

                int hour = GetByte(this.Offset + 4);
                if (hour < 0 || hour > 24) { break; }

                int min = GetByte(this.Offset + 5);
                if (min < 0 || min > 60) { break; }

                int sec = GetByte(this.Offset + 6);
                if (sec < 0 || sec > 60) { break; }

                res = new DateTime(year, mon, day, hour, min, sec);
                break;
            }

            return res;
        }

        public DateTime GetDateTime6B()
        {
            var res = DateTime.MinValue;
            var len = this.limit - this.Offset;
            if (len < 6)
                return res;

            while (res == DateTime.MinValue)
            {
                int year = GetByte(this.Offset);
                if (year >= 0x50)
                {
                    year = 1900 + year;
                }
                else
                {
                    year = 2000 + year;
                }

                int mon = GetByte(this.Offset + 1);
                if (mon < 1 || mon > 12) { break; }

                int day = GetByte(this.Offset + 2);
                if (day < 1 || day > 31) { break; }

                int hour = GetByte(this.Offset + 3);
                if (hour < 0 || hour > 24) { break; }

                int min = GetByte(this.Offset + 4);
                if (min < 0 || min > 60) { break; }

                int sec = GetByte(this.Offset + 5);
                if (sec < 0 || sec > 60) { break; }

                res = new DateTime(year, mon, day, hour, min, sec);
                break;
            }

            return res;
        }

        public DateTime GetDateTime6BDec()
        {
            var res = DateTime.MinValue;
            var len = this.limit - this.Offset;
            if (len < 6)
                return res;

            string strDT = ToHex();
            Int64 valid = 0;
            if (strDT.Length != 12 || string.IsNullOrEmpty(strDT) || !Int64.TryParse(strDT, out valid))
            {
                return res;
            }

            while (res == DateTime.MinValue)
            {
                int year = -1;
                if (!int.TryParse(strDT.Substring(0, 2), out year)) break;
                year += 2000;

                int mon = -1;
                if (!int.TryParse(strDT.Substring(2, 2), out mon)) break;
                if (mon < 1 || mon > 12) { break; }

                int day = -1;
                if (!int.TryParse(strDT.Substring(4, 2), out day)) break;
                if (day < 1 || day > 31) { break; }

                int hour = -1;
                if (!int.TryParse(strDT.Substring(6, 2), out hour)) break; 
                if (hour < 0 || hour > 24) { break; }

                int min = -1;
                if (!int.TryParse(strDT.Substring(8, 2), out min)) break; 
                if (min < 0 || min > 60) { break; }

                int sec = -1;
                if (!int.TryParse(strDT.Substring(10, 2), out sec)) break; 
                if (sec < 0 || sec > 60) { break; }

                res = new DateTime(year, mon, day, hour, min, sec);
                break;
            }

            return res;
        }

        public DateTime GetDate4B()
        {
            var res = DateTime.MinValue;
            var len = this.limit - this.Offset;
            if (len < 4)
                return res;

            while (res == DateTime.MinValue)
            {
                int year = GetUInt16LE(this.Offset);
                if (year < 0) { break; }

                int mon = GetByte(this.Offset + 2);
                if (mon < 1 || mon > 12) { break; }

                int day = GetByte(this.Offset + 3);
                if (day < 1 || day > 31) { break; }

                res = new DateTime(year, mon, day);
                break;
            }

            return res;
        }

        public DateTime GetDate4B_RevYear()
        {
            var res = DateTime.MinValue;
            var len = this.limit - this.Offset;
            if (len < 4)
                return res;

            while (res == DateTime.MinValue)
            {
                int year = GetUInt16BE(this.Offset);
                if (year < 0) { break; }

                int mon = GetByte(this.Offset + 2);
                if (mon < 1 || mon > 12) { break; }

                int day = GetByte(this.Offset + 3);
                if (day < 1 || day > 31) { break; }

                res = new DateTime(year, mon, day);
                break;
            }

            return res;
        }
        #endregion

        public Tuple<int, int> GetNibbles(int at=-1)
        {
            var tmp = this.GetByte(at);
            var hi = (tmp & 0xF0) >> 4;
            var lo = (tmp & 0x0F);

            return Tuple.Create(hi, lo);
        }

        public byte GetByte(int at = -1)
        {
            if (this.Offset + 1 > this.data.Length)
                throw new IndexOutOfRangeException("The (index, size) pair is not valid");

            var here = advance(at, 1);
            return this.data[here];
        }

        public sbyte GetSByte(int at = -1)
        {
            if (this.Offset + 1 > this.data.Length)
                throw new IndexOutOfRangeException("The (index, size) pair is not valid");

            var here = advance(at, 1);
            return (sbyte)this.data[here];
        }

        public byte[] GetBytes(int size, int at = -1)
        {
            if (this.Offset + size > this.data.Length)
                throw new IndexOutOfRangeException("The (index, size) pair is not valid");

            var bytes = new byte[size];
            var here = advance(at, size);
            Buffer.BlockCopy(this.data, here, bytes, 0, size);
            return bytes;
        }

        public byte[] GetBytesUntil(byte value)
        {
            int index = -1;
            for (int i=this.Offset; i<this.data.Length; i++)
            {
                if (this.data[i] == value)
                {
                    index = i;
                    break;
                }
            }

            int count = index - this.Offset;
            if (count <= 0) return null;

            var bytes = new byte[count]; 
            Buffer.BlockCopy(this.data, this.Offset, bytes, 0, count);

            return bytes;
        }

        public DateTime GetSystemTime(int at = -1)
        {
            Int16 year, month, dayOfWeek, day, hour, minute, seconds, milliSecs;

            if (at == -1)
            {
                year      = this.GetInt16LE();
                month     = this.GetInt16LE();
                dayOfWeek = this.GetInt16LE();
                day       = this.GetInt16LE();
                hour      = this.GetInt16LE();
                minute    = this.GetInt16LE();
                seconds   = this.GetInt16LE();
                milliSecs = this.GetInt16LE();
            }
            else
            {
                year      = this.GetInt16LE(at);
                month     = this.GetInt16LE(at + 2);
                dayOfWeek = this.GetInt16LE(at + 4);
                day       = this.GetInt16LE(at + 6);
                hour      = this.GetInt16LE(at + 8);
                minute    = this.GetInt16LE(at + 10);
                seconds   = this.GetInt16LE(at + 12);
                milliSecs = this.GetInt16LE(at + 14);
            }

            return new DateTime(year,month, day, hour, minute, seconds, milliSecs);
        }

        public FILETIME GetFileTime(int at = -1)
        {
            var t0 = (Int64)this.GetUInt32LE();
            var tt = t0 * 10000000 + 116444736000000000;

            var ft = new FILETIME();
            ft.dwLowDateTime  = (int)tt;
            ft.dwHighDateTime = (int)(tt >> 32);

            return ft;
        }

        public FILETIME GetFileTimeBy(int length, int at = -1)
        {
            UInt64 t0 = 0;
            if (length == 4)
            {
                t0 = (UInt64)this.GetUInt32LE();
                Int64 tt = (Int64)(t0 * 10000000 + 116444736000000000);

                var ft = new FILETIME();
                ft.dwLowDateTime = (int)tt;
                ft.dwHighDateTime = (int)(tt >> 32);
                return ft;
            }
            else
            {   // length == 8
                var ft0 = new FILETIME();
                ft0.dwLowDateTime = (int)this.GetUInt32LE();
                ft0.dwHighDateTime = (int)this.GetUInt32LE();
                return ft0;
            }
        }

        public string GetStringUTF8(int size)
        {
            return Encoding.UTF8.GetString(GetBytes(size));
        }

        public ByteBuffer2 SetUnicode(string s, bool addNullAtEmpty = false)
        {
            if (!String.IsNullOrEmpty(s))
            {
                var bytes = Encoding.Unicode.GetBytes(s);
                Buffer.BlockCopy(bytes, 0, this.data, this.Offset, bytes.Length);
                this.Offset += 2 * s.Length;

                addNullAtEmpty = true;
            }

            if (addNullAtEmpty)
            {
                this.data[this.Offset] = 0; this.Offset++;
                this.data[this.Offset] = 0; this.Offset++;
            }

            return this;
        }

        public ByteBuffer2 SetInt16BE(short v)
        {
            var buffer = BitConverter.GetBytes(v);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            this.UpdateBuffer(buffer, 0, buffer.Length);
            this.Offset += 2;

            return this;
        }

        public ByteBuffer2 SetUInt16BE(ushort v)
        {
            var buffer = BitConverter.GetBytes(v);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            this.UpdateBuffer(buffer, 0, buffer.Length);
            this.Offset += 2;

            return this;
        }

        public ByteBuffer2 SetUInt16LE(ushort v)
        {
            var buffer = BitConverter.GetBytes(v);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            this.UpdateBuffer(buffer, 0, buffer.Length);
            this.Offset += 2;

            return this;
        }

        public ByteBuffer2 SetInt32BE(int v)
        {
            var buffer = BitConverter.GetBytes(v);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            this.UpdateBuffer(buffer, 0, buffer.Length);
            this.Offset += 4;

            return this;
        }

        public ByteBuffer2 SetUInt32BE(uint v)
        {
            var buffer = BitConverter.GetBytes(v);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            this.UpdateBuffer(buffer, 0, buffer.Length);
            this.Offset += 4;

            return this;
        }

        public ByteBuffer2 SetUInt32LE(uint v)
        {
            var buffer = BitConverter.GetBytes(v);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            this.UpdateBuffer(buffer, 0, buffer.Length);
            this.Offset += 4;

            return this;
        }

        public ByteBuffer2 SetUInt64BE(ulong v)
        {
            var buffer = BitConverter.GetBytes(v);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            this.UpdateBuffer(buffer, 0, buffer.Length);
            this.Offset += 8;

            return this;
        }

        public ByteBuffer2 SetUInt64LE(ulong v)
        {
            var buffer = BitConverter.GetBytes(v);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            this.UpdateBuffer(buffer, 0, buffer.Length);
            this.Offset += 8;

            return this;
        }

        public ByteBuffer2 SetByte(byte b)
        {
            var bytes = new byte[] { b };

            return this.SetBytes(bytes);
        }

        public ByteBuffer2 SetBytes(byte[] buffer)
        {
            SetBytes(buffer, buffer.Length);

            return this;
        }

        public ByteBuffer2 SetBytes(byte[] buffer, int size)
        {
            this.UpdateBuffer(buffer, 0, size);
            this.Offset += size;

            return this;
        }

        public ByteBuffer2 SetStringUTF8(string s)
        {
            var buffer = Encoding.UTF8.GetBytes(s);

            this.UpdateBuffer(buffer, 0, buffer.Length);
            this.Offset += buffer.Length;

            return this;
        }

        public ByteBuffer2 Unget(int count)
        {
            this.Offset -= count;
            return this;
        }

        public ByteBuffer2 Skip(int count)
        {
            this.Offset += count;

            return this;
        }

        public ByteBuffer2 Append(byte b)
        {
            var bytes = new byte[] { b };

            return this.Append(bytes, 0, 1);
        }

        public ByteBuffer2 Append(ByteBuffer2 buffer)
        {
            var data   = buffer.data;
            var offset = buffer.Offset;
            var count  = buffer.RemainedSize;
            return this.Append(data, offset, count);
        }

        public ByteBuffer2 UpdateBuffer(byte[] buffer, int offset, int count)
        {
            if (buffer != null && count > 0)
            {
                var len = (this.data == null) ? 0 : this.Offset;

                if (this.limit - this.Offset >= count)
                {
                    Buffer.BlockCopy(buffer, offset, this.data, this.Offset, count);   // copy previous buffer
                }
                else
                {
                    var newBuf = new byte[len + count];
                    if (this.data != null)
                    {
                        Buffer.BlockCopy(this.data, 0, newBuf, 0, len);   // copy previous buffer
                    }

                    Buffer.BlockCopy(buffer, offset, newBuf, len, count); // append new buffer
                    this.data = newBuf;
                    this.count = len + count;
                    this.limit = len + count;
                }
            }

            return this;
        }

        public ByteBuffer2 Append(byte[] buffer, int offset, int count)
        {
            if (buffer != null && count > 0)
            { 
                var len = (this.data == null) ? 0 : this.data.Length;

                var newBuf = new byte[len + count];
                if (this.data != null)
                { 
                    Buffer.BlockCopy(this.data, 0, newBuf, 0, len);   // copy previous buffer
                }

                Buffer.BlockCopy(buffer, offset, newBuf, len, count); // append new buffer
                this.data  = newBuf;
                this.count += count;
                this.limit += count;
            }

            return this;
        }

        public ByteBuffer2 Slice(int offset, int count)
        {
            return new ByteBuffer2(this.CopyRange(offset, count));
        }

        public bool CompareRange(int offset, int count, byte value)
        {
            for (int i=offset; i<offset+count; i++)
                if (this.data[i] != value)
                    return false;

            return true;
        }

        public bool CompareRange(int offset, int count, byte[] value)
        {
            if (this.data.Length < offset + count || value.Length < count)
                return false;

            for (int i = 0; i < count; i++)
                if (this.data[i + offset] != value[i])
                    return false;

            return true;
        }

        public byte[] CopyRange(int from, int count)
        {
            /*
            if (from + count > this.limit)
                throw new IndexOutOfRangeException("ByteBuffer2.CopyRange: buffer out of range");
            */

            var result = new byte[count];
            Buffer.BlockCopy(this.data, from, result, 0, count);
            return result;
        }

        public byte[] ComputeHash(int from, int to)
        {
            if (from > to || to > this.limit)
                throw new Exception("ByteBuffer2.ComputeHash: wrong buffer boundary");

            var sha1 = new SHA1CryptoServiceProvider();
            return sha1.ComputeHash(this.data, from, to - from);
        }

        private byte leadingByte(byte b)
        {
            return ((int)b & 0x80) == 0 ? (byte)0 : (byte)0xFF;
        }

        private int advance(int at, int dist)
        {
            var here = (at == -1) ? this.Offset : this.begin + at;
            if (at == -1)
                this.Offset += dist;

            return here;
        }

        public override string ToString()
        {
            return string.Format("begin: 0x{0:x}, offset: 0x{1:x}, remained: 0x{2:x}, limit: 0x{3:x}", 
                begin, Offset, RemainedSize, limit);
        }
    }
}
