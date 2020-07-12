using System;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;

namespace AyameFS {
    public struct MD5Hash: IEquatable<MD5Hash> {
        public readonly ulong lsb, msb;

        public byte[] Bytes {
            get {
                using(var ms = new MemoryStream(16))
                using(var bw = new BinaryWriter(ms)) {
                    bw.Write(lsb);
                    bw.Write(msb);
                    return ms.ToArray();
                }
            }
        }

        public MD5Hash(string source) {
            if(source == null)
                throw new ArgumentNullException(nameof(source));
            lsb = ulong.Parse(source.Substring(0, 16), NumberStyles.HexNumber);
            msb = ulong.Parse(source.Substring(16), NumberStyles.HexNumber);
        }

        public MD5Hash(byte[] source) {
            if(source == null)
                throw new ArgumentNullException(nameof(source));
            if(source.Length < 16)
                throw new ArgumentOutOfRangeException(nameof(source));
            using(var ms = new MemoryStream(source))
            using(var br = new BinaryReader(ms)) {
                lsb = br.ReadUInt64();
                msb = br.ReadUInt64();
            }
        }

        public MD5Hash(ulong lsb, ulong msb) {
            this.lsb = lsb;
            this.msb = msb;
        }

        public bool Equals(MD5Hash other) =>
            lsb == other.lsb && msb == other.msb;

        public override bool Equals(object obj) =>
            obj is MD5Hash other && Equals(other);

        public override int GetHashCode() =>
            unchecked((int)(lsb ^ msb ^ (lsb >> 32) ^ (msb >> 32)));

        public override string ToString() =>
            lsb.ToString("X16") + msb.ToString("X16");

        public static MD5Hash Compute(byte[] bytes) {
            using(var md5 = MD5.Create())
                return new MD5Hash(md5.ComputeHash(bytes));
        }

        public static MD5Hash Compute(byte[] bytes, int offset, int count) {
            using(var md5 = MD5.Create())
                return new MD5Hash(md5.ComputeHash(bytes, offset, count));
        }

        public static MD5Hash Compute(Stream stream) {
            using(var md5 = MD5.Create())
                return new MD5Hash(md5.ComputeHash(stream));
        }
    }
}
