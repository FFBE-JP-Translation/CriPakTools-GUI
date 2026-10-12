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
    public static class FfbeJpConverter
    {
        /// <param name="toFfbeJp">true: regular -> FFBE JP (encrypt), false: FFBE JP -> regular (decrypt)</param>
        /// <returns>number of files that were converted</returns>
        public static int Convert(string inputCpk, string outputCpk, bool toFfbeJp, Action<float> onProgress = null)
        {
            if (string.Equals(Path.GetFullPath(inputCpk), Path.GetFullPath(outputCpk), StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Input and output CPK must be different files.");
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
