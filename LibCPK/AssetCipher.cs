using System;
using System.Text;

namespace LibCPK
{
    /// <summary>
    /// Per-file obfuscation used by the "new" CPK format (the Japanese FFBE CPKs).
    ///
    /// In this format every file stored in the CPK is scrambled with a key derived from
    /// the file's own name (no directory part). The CPK container itself is an ordinary
    /// CRIWARE CPK; only the payload of each file differs.
    ///
    /// Decryption (what the game / DataExtractor does):
    ///   key  : h = 0; for each byte c of the ASCII file name: h = h * 31 + c   (uint32)
    ///   ks(i): ((h >> (8 * (i &amp; 3))) &amp; 0xFF) + (i % 255)                       (byte)
    ///   half = size / 2, second = half + (size &amp; 1)   (a middle byte of an odd sized file is untouched)
    ///   for i in [0, half):
    ///       a = data[i]; b = data[second + i]
    ///       data[i]          = ks(i) ^ b
    ///       data[second + i] = a ^ ~ks(i)
    ///
    /// Encryption is the exact inverse of that transform.
    /// </summary>
    public static class AssetCipher
    {
        /// <summary>
        /// Files whose name contains this text are stored as-is in the original archives.
        /// </summary>
        private const string UnencryptedMarker = "unit_unit1";

        /// <summary>
        /// Returns true when the file is subject to the per-file scrambling.
        /// </summary>
        public static bool AppliesTo(string fileName)
        {
            return fileName != null && !fileName.Contains(UnencryptedMarker);
        }

        public static byte[] Decrypt(byte[] data, string fileName)
        {
            if (!AppliesTo(fileName))
            {
                return data;
            }

            byte[] result = (byte[])data.Clone();
            uint hash = KeyHash(fileName);
            int half = result.Length >> 1;
            int second = half + (result.Length & 1);

            for (int i = 0; i < half; i++)
            {
                byte k = KeyByte(hash, i);
                byte a = result[i];
                byte b = result[second + i];
                result[i] = (byte)(k ^ b);
                result[second + i] = (byte)(a ^ ~k);
            }

            return result;
        }

        public static byte[] Encrypt(byte[] data, string fileName)
        {
            if (!AppliesTo(fileName))
            {
                return data;
            }

            byte[] result = (byte[])data.Clone();
            uint hash = KeyHash(fileName);
            int half = result.Length >> 1;
            int second = half + (result.Length & 1);

            for (int i = 0; i < half; i++)
            {
                byte k = KeyByte(hash, i);
                byte p1 = result[i];
                byte p2 = result[second + i];
                result[i] = (byte)(p2 ^ ~k);
                result[second + i] = (byte)(p1 ^ k);
            }

            return result;
        }

        private static uint KeyHash(string fileName)
        {
            // The original implementation hands a NUL terminated ASCII string to native code.
            byte[] key = Encoding.ASCII.GetBytes(fileName);
            uint hash = 0;
            for (int i = 0; i < key.Length && key[i] != 0; i++)
            {
                hash = unchecked(hash * 31 + key[i]);
            }
            return hash;
        }

        private static byte KeyByte(uint hash, int index)
        {
            return unchecked((byte)(((hash >> (8 * (index & 3))) & 0xFF) + (index % 255)));
        }
    }
}
