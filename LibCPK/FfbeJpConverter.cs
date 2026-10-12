using System;
using System.IO;
using System.Linq;

namespace LibCPK
{
    /// <summary>
    /// Converts a CPK between the regular format and the FFBE JP format.
    /// The only difference between them is the per file encryption (see <see cref="AssetCipher"/>), which does not
    /// change any size, so the CPK is copied and the stored data of every file is transformed in place.
    /// </summary>
    public enum CpkFormat
    {
        /// <summary>Not enough recognisable data to tell.</summary>
        Unknown,
        Regular,
        FfbeJp
    }

    public static class FfbeJpConverter
    {
        private const int PrefixLength = 16;

        private static readonly string[] Magics =
        {
            "\x89PNG", "CRILAYLA", "OggS", "RIFF", "DDS ", "PK\x03\x04", "@UTF", "CRID", "GIF8", "BM", "\xFF\xD8\xFF",
            "AFS2", "CPK ", "ID3", "\x7F" + "ELF", "#!", "<?xml", "{", "[", "\xEF\xBB\xBF"
        };

        private static bool LooksValid(byte[] head)
        {
            string text = System.Text.Encoding.GetEncoding(28591).GetString(head);
            foreach (string magic in Magics)
            {
                if (text.StartsWith(magic, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            // plain text (csv, json, ...)
            return head.All(b => b == 9 || b == 10 || b == 13 || (b >= 0x20 && b < 0x7F));
        }

        /// <summary>
        /// Tells whether the files of a CPK are stored encrypted (FFBE JP) or as they are (regular) by checking
        /// which variant of the first bytes of the files looks like real data (known file signatures or text).
        /// </summary>
        public static CpkFormat Detect(string cpkPath)
        {
            CPK cpk = new CPK();
            if (!cpk.ReadCPK(cpkPath))
            {
                throw new InvalidDataException("Not a valid CPK: " + cpkPath);
            }

            int regularVotes = 0, ffbeJpVotes = 0;
            using (FileStream fs = File.OpenRead(cpkPath))
            {
                foreach (FileEntry e in cpk.fileTable.Where(x => x.FileType == "FILE"))
                {
                    string name = e.FileName == null ? "" : e.FileName.ToString();
                    long size = System.Convert.ToInt64(e.FileSize);
                    if (name.Length == 0 || !AssetCipher.AppliesTo(name) || size < PrefixLength * 2)
                    {
                        continue;
                    }

                    long offset = (long)e.FileOffset;
                    byte[] head = ReadAt(fs, offset, PrefixLength);
                    byte[] middle = ReadAt(fs, offset + size / 2 + (size & 1), PrefixLength);
                    bool regular = LooksValid(head);
                    bool ffbeJp = LooksValid(AssetCipher.DecryptPrefix(middle, name));

                    if (regular && !ffbeJp) regularVotes++;
                    else if (ffbeJp && !regular) ffbeJpVotes++;
                }
            }

            if (ffbeJpVotes > regularVotes) return CpkFormat.FfbeJp;
            if (regularVotes > ffbeJpVotes) return CpkFormat.Regular;
            return CpkFormat.Unknown;
        }

        private static byte[] ReadAt(FileStream fs, long offset, int length)
        {
            byte[] buffer = new byte[length];
            fs.Seek(offset, SeekOrigin.Begin);
            int read = 0;
            while (read < length)
            {
                int n = fs.Read(buffer, read, length - read);
                if (n <= 0)
                {
                    break;
                }
                read += n;
            }
            return buffer;
        }

        /// <param name="toFfbeJp">true: regular -> FFBE JP (encrypt), false: FFBE JP -> regular (decrypt)</param>
        /// <returns>number of files that were converted</returns>
        /// <param name="force">convert even if the detected format is not the expected source format</param>
        public static int Convert(string inputCpk, string outputCpk, bool toFfbeJp, Action<float> onProgress = null, bool force = false)
        {
            if (string.Equals(Path.GetFullPath(inputCpk), Path.GetFullPath(outputCpk), StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Input and output CPK must be different files.");
            }

            if (!force)
            {
                CpkFormat format = Detect(inputCpk);
                if (format == (toFfbeJp ? CpkFormat.FfbeJp : CpkFormat.Regular))
                {
                    throw new InvalidOperationException("The CPK is already in the " + (toFfbeJp ? "FFBE JP" : "regular") + " format, converting it again would corrupt it. Use force to override.");
                }
                if (format == CpkFormat.Unknown)
                {
                    throw new InvalidOperationException("Could not tell which format the CPK is in (no recognisable file data). Use force to convert anyway.");
                }
            }

            CPK cpk = new CPK();
            if (!cpk.ReadCPK(inputCpk))
            {
                throw new InvalidDataException("Not a valid CPK: " + inputCpk);
            }
            if (cpk.isNamelessPack)
            {
                throw new NotSupportedException("Nameless packs can not be converted.");
            }

            var files = cpk.fileTable.Where(x => x.FileType == "FILE").ToList();
            if (files.Any(x => x.FileName == null || x.FileName.ToString().Length == 0))
            {
                throw new NotSupportedException("This CPK has no file names (ID only), the file name key can not be derived.");
            }

            File.Copy(inputCpk, outputCpk, true);
            int count = 0;
            using (FileStream fs = new FileStream(outputCpk, FileMode.Open, FileAccess.ReadWrite))
            {
                for (int i = 0; i < files.Count; i++)
                {
                    onProgress?.Invoke((float)i / files.Count * 100f);

                    string name = files[i].FileName.ToString();
                    if (!AssetCipher.AppliesTo(name))
                    {
                        continue;
                    }

                    int size = System.Convert.ToInt32(files[i].FileSize);
                    byte[] data = new byte[size];
                    fs.Seek((long)files[i].FileOffset, SeekOrigin.Begin);
                    int read = 0;
                    while (read < size)
                    {
                        int n = fs.Read(data, read, size - read);
                        if (n <= 0)
                        {
                            throw new EndOfStreamException("Unexpected end of CPK in " + name);
                        }
                        read += n;
                    }

                    data = toFfbeJp ? AssetCipher.Encrypt(data, name) : AssetCipher.Decrypt(data, name);
                    fs.Seek((long)files[i].FileOffset, SeekOrigin.Begin);
                    fs.Write(data, 0, data.Length);
                    count++;
                }
            }
            onProgress?.Invoke(100f);
            return count;
        }
    }
}
