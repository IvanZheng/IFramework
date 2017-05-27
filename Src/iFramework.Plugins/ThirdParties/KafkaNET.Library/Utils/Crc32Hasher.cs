using System;
using System.Security.Cryptography;

namespace Kafka.Client.Utils
{
    /// <summary>
    ///     From http://damieng.com/blog/2006/08/08/calculating_crc32_in_c_and_net
    /// </summary>
    public class Crc32Hasher : HashAlgorithm
    {
        internal const uint DefaultPolynomial = 0xedb88320;
        internal const uint DefaultSeed = 0xffffffff;
        private static uint[] defaultTable;

        private uint hash;
        private readonly uint seed;
        private readonly uint[] table;

        public Crc32Hasher()
        {
            table = InitializeTable(DefaultPolynomial);
            seed = DefaultSeed;
            Initialize();
        }

        [CLSCompliant(false)]
        public Crc32Hasher(uint polynomial, uint seed)
        {
            table = InitializeTable(polynomial);
            this.seed = seed;
            Initialize();
        }

        public override int HashSize => 32;

        public override void Initialize()
        {
            hash = seed;
        }

        protected override void HashCore(byte[] buffer, int start, int length)
        {
            hash = CalculateHash(table, hash, buffer, start, length);
        }

        protected override byte[] HashFinal()
        {
            var hashBuffer = UInt32ToBigEndianBytes(~hash);
            HashValue = hashBuffer;
            return hashBuffer;
        }

        public static byte[] Compute(byte[] bytes)
        {
            var hasher = new Crc32Hasher();
            var hash = hasher.ComputeHash(bytes);
            return hash;
        }

        internal static uint ComputeCrcUint32(byte[] bytes, int offset, int count)
        {
            var hasher = new Crc32Hasher();
            return ~CalculateHash(InitializeTable(DefaultPolynomial), DefaultSeed, bytes, offset, count);
        }

        private static uint[] InitializeTable(uint polynomial)
        {
            if (polynomial == DefaultPolynomial && defaultTable != null)
                return defaultTable;

            var createTable = new uint[256];
            for (var i = 0; i < 256; i++)
            {
                var entry = (uint) i;
                for (var j = 0; j < 8; j++)
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ polynomial;
                    else
                        entry = entry >> 1;
                createTable[i] = entry;
            }

            if (polynomial == DefaultPolynomial)
                defaultTable = createTable;

            return createTable;
        }

        private static uint CalculateHash(uint[] table, uint seed, byte[] buffer, int start, int size)
        {
            var crc = seed;
            for (var i = start; i < start + size; i++)
                unchecked
                {
                    crc = (crc >> 8) ^ table[buffer[i] ^ (crc & 0xff)];
                }
            return crc;
        }

        private byte[] UInt32ToBigEndianBytes(uint x)
        {
            return new[]
            {
                (byte) ((x >> 24) & 0xff),
                (byte) ((x >> 16) & 0xff),
                (byte) ((x >> 8) & 0xff),
                (byte) (x & 0xff)
            };
        }
    }
}