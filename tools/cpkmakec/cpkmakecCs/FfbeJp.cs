using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace cpkmakecCs;

/// <summary>
/// FFBE JP CPK format: every file stored in the CPK is encrypted with a key derived from its own file name.
/// The data of a file is first compressed (CRILAYLA, optional) and then encrypted, so the encryption is applied to the
/// finished CPK: packing builds the CPK normally and then encrypts every stored file in place, extracting decrypts a
/// temporary copy of the CPK and lets the normal extraction (with its decompression) work on that.
/// Encryption does not change any size, so the CPK tables stay valid.
///
///   hash  : h = 0; for each byte c of the ASCII file name: h = h * 31 + c   (uint32)
///   ks(i) : ((h >> (8 * (i & 3))) & 0xFF) + (i % 255)                    (byte)
///   half = size / 2, second = half + (size & 1)
///   decrypt, for i in [0, half):  a = d[i]; b = d[second + i]; d[i] = ks ^ b; d[second + i] = a ^ ~ks
///   encrypt is the inverse of that.
/// </summary>
public static class FfbeJp
{
	/// <summary>Files whose name contains this text are stored as they are.</summary>
	private const string UnencryptedMarker = "unit_unit1";

	public static bool AppliesTo(string fileName)
	{
		return fileName != null && !fileName.Contains(UnencryptedMarker);
	}

	/// <summary>The key is the file name without any directory.</summary>
	public static string KeyName(string path)
	{
		int i = path.LastIndexOfAny(new char[] { '\\', '/' });
		return i < 0 ? path : path.Substring(i + 1);
	}

	private static uint KeyHash(string fileName)
	{
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
		return unchecked((byte)(((hash >> 8 * (index & 3)) & 0xFF) + index % 255));
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

	/// <summary>Decrypts the first bytes of a file from the stored bytes starting at offset size / 2 + size % 2.</summary>
	public static byte[] DecryptPrefix(byte[] middle, string fileName)
	{
		uint hash = KeyHash(fileName);
		byte[] result = new byte[middle.Length];
		for (int i = 0; i < middle.Length; i++)
		{
			result[i] = (byte)(KeyByte(hash, i) ^ middle[i]);
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

	private class Entry
	{
		public string Name;
		public long Offset;
		public int Size;
	}

	private static uint ReadU32(byte[] b, int p)
	{
		return (uint)((b[p] << 24) | (b[p + 1] << 16) | (b[p + 2] << 8) | b[p + 3]);
	}

	private static ushort ReadU16(byte[] b, int p)
	{
		return (ushort)((b[p] << 8) | b[p + 1]);
	}

	private static ulong ReadU64(byte[] b, int p)
	{
		return ((ulong)ReadU32(b, p) << 32) | ReadU32(b, p + 4);
	}

	/// <summary>Reads a packet ("CPK ", "TOC ", ...) and returns its (unmasked) utf table.</summary>
	private static byte[] ReadPacket(FileStream fs, long offset, string id)
	{
		byte[] head = new byte[16];
		fs.Seek(offset, SeekOrigin.Begin);
		if (fs.Read(head, 0, 16) != 16 || Encoding.ASCII.GetString(head, 0, 4) != id)
		{
			throw new InvalidDataException("'" + id.Trim() + "' packet not found at " + offset);
		}
		long size = (long)BitConverter.ToUInt64(head, 8);
		byte[] utf = new byte[size];
		int read = 0;
		while (read < utf.Length)
		{
			int n = fs.Read(utf, read, utf.Length - read);
			if (n <= 0)
			{
				throw new EndOfStreamException();
			}
			read += n;
		}
		if (!(utf[0] == 0x40 && utf[1] == 0x55 && utf[2] == 0x54 && utf[3] == 0x46))
		{
			// masked table
			int x = 25951;
			for (int i = 0; i < utf.Length; i++)
			{
				utf[i] = (byte)(utf[i] ^ (x & 0xFF));
				x = unchecked(x * 16661);
			}
		}
		return utf;
	}

	/// <summary>Minimal @UTF table reader: returns the rows as column name -> value (string or ulong).</summary>
	private static List<Dictionary<string, object>> ReadUtf(byte[] u)
	{
		int baseOff = 8;
		int rowOff = ReadU16(u, 10);
		uint strOff = ReadU32(u, 12);
		int columns = ReadU16(u, 24);
		int rowSize = ReadU16(u, 26);
		uint rows = ReadU32(u, 28);

		Func<uint, string> str = delegate(uint o)
		{
			int a = (int)(baseOff + strOff + o);
			int e = a;
			while (u[e] != 0)
			{
				e++;
			}
			return Encoding.UTF8.GetString(u, a, e - a);
		};

		int p = 32;
		string[] names = new string[columns];
		int[] flags = new int[columns];
		object[] constants = new object[columns];
		for (int i = 0; i < columns; i++)
		{
			flags[i] = u[p++];
			names[i] = str(ReadU32(u, p));
			p += 4;
			if ((flags[i] & 0xF0) == 0x30)
			{
				int len;
				constants[i] = ReadValue(u, p, flags[i] & 0x0F, str, out len);
				p += len;
			}
		}

		List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
		for (uint r = 0; r < rows; r++)
		{
			int q = baseOff + rowOff + (int)r * rowSize;
			Dictionary<string, object> row = new Dictionary<string, object>();
			for (int i = 0; i < columns; i++)
			{
				int storage = flags[i] & 0xF0;
				if (storage == 0x30)
				{
					row[names[i]] = constants[i];
				}
				else if (storage == 0x50)
				{
					int len;
					row[names[i]] = ReadValue(u, q, flags[i] & 0x0F, str, out len);
					q += len;
				}
			}
			result.Add(row);
		}
		return result;
	}

	private static object ReadValue(byte[] u, int p, int type, Func<uint, string> str, out int len)
	{
		switch (type)
		{
		case 0:
		case 1:
			len = 1;
			return (ulong)u[p];
		case 2:
		case 3:
			len = 2;
			return (ulong)ReadU16(u, p);
		case 4:
		case 5:
		case 8:
			len = 4;
			return (ulong)ReadU32(u, p);
		case 6:
		case 7:
		case 9:
			len = 8;
			return ReadU64(u, p);
		case 10:
			len = 4;
			return str(ReadU32(u, p));
		default:
			len = 8;
			return null;
		}
	}

	private static List<Entry> ReadEntries(FileStream fs)
	{
		byte[] header = ReadPacket(fs, 0, "CPK ");
		Dictionary<string, object> cpk = ReadUtf(header)[0];
		if (!cpk.ContainsKey("TocOffset") || cpk["TocOffset"] == null)
		{
			throw new InvalidDataException("This CPK has no file names (ID only mode), the file name key cannot be derived.");
		}
		long toc = (long)(ulong)cpk["TocOffset"];
		long content = cpk.ContainsKey("ContentOffset") && cpk["ContentOffset"] != null ? (long)(ulong)cpk["ContentOffset"] : toc;
		long add = toc > 0x800 ? 0x800 : toc;
		if (content < add)
		{
			add = content;
		}

		List<Entry> entries = new List<Entry>();
		foreach (Dictionary<string, object> row in ReadUtf(ReadPacket(fs, toc, "TOC ")))
		{
			if (!row.ContainsKey("FileName") || row["FileName"] == null)
			{
				throw new InvalidDataException("This CPK has no file names (ID only mode), the file name key cannot be derived.");
			}
			Entry e = new Entry();
			e.Name = (string)row["FileName"];
			e.Offset = add + (long)(ulong)row["FileOffset"];
			e.Size = (int)(ulong)row["FileSize"];
			entries.Add(e);
		}
		return entries;
	}

	/// <summary>
	/// Encrypts (or decrypts) the stored data of every file of the CPK in place.
	/// Returns the number of files that were processed.
	/// </summary>
	public static int ProcessCpk(string cpkPath, bool encrypt)
	{
		int count = 0;
		using (FileStream fs = new FileStream(cpkPath, FileMode.Open, FileAccess.ReadWrite))
		{
			foreach (Entry e in ReadEntries(fs))
			{
				string key = KeyName(e.Name);
				if (!AppliesTo(key) || e.Size == 0)
				{
					continue;
				}
				byte[] data = new byte[e.Size];
				fs.Seek(e.Offset, SeekOrigin.Begin);
				int read = 0;
				while (read < data.Length)
				{
					int n = fs.Read(data, read, data.Length - read);
					if (n <= 0)
					{
						throw new EndOfStreamException("Unexpected end of CPK in " + e.Name);
					}
					read += n;
				}
				data = encrypt ? Encrypt(data, key) : Decrypt(data, key);
				fs.Seek(e.Offset, SeekOrigin.Begin);
				fs.Write(data, 0, data.Length);
				count++;
			}
		}
		return count;
	}

	private static readonly string[] Magics =
	{
		"\u0089PNG", "CRILAYLA", "OggS", "RIFF", "DDS ", "PK\u0003\u0004", "@UTF", "CRID", "GIF8", "BM", "\u00FF\u00D8\u00FF",
		"AFS2", "CPK ", "ID3", "\u007FELF", "#!", "<?xml", "{", "[", "\u00EF\u00BB\u00BF"
	};

	private static bool LooksValid(byte[] head)
	{
		string text = Encoding.GetEncoding(28591).GetString(head);
		foreach (string magic in Magics)
		{
			if (text.StartsWith(magic, StringComparison.Ordinal))
			{
				return true;
			}
		}
		foreach (byte b in head)
		{
			if (!(b == 9 || b == 10 || b == 13 || (b >= 0x20 && b < 0x7F)))
			{
				return false;
			}
		}
		return true;
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

	/// <summary>
	/// Tells whether the files of a CPK are stored encrypted (FFBE JP) or as they are (regular) by checking which
	/// variant of the first bytes of the files looks like real data (known signatures or text).
	/// Returns 1 for FFBE JP, 0 for regular and -1 if it cannot be told.
	/// </summary>
	public static int DetectFormat(string cpkPath)
	{
		int regularVotes = 0;
		int ffbeJpVotes = 0;
		using (FileStream fs = File.OpenRead(cpkPath))
		{
			foreach (Entry e in ReadEntries(fs))
			{
				string key = KeyName(e.Name);
				if (!AppliesTo(key) || e.Size < 32)
				{
					continue;
				}
				byte[] head = ReadAt(fs, e.Offset, 16);
				byte[] middle = ReadAt(fs, e.Offset + e.Size / 2 + (e.Size & 1), 16);
				byte[] plain = DecryptPrefix(middle, key);
				bool regular = LooksValid(head);
				bool ffbeJp = LooksValid(plain);
				if (regular && !ffbeJp)
				{
					regularVotes++;
				}
				else if (ffbeJp && !regular)
				{
					ffbeJpVotes++;
				}
			}
		}
		return ffbeJpVotes > regularVotes ? 1 : (regularVotes > ffbeJpVotes ? 0 : -1);
	}

	/// <summary>Creates a temporary copy of the CPK; it is deleted when the program ends.</summary>
	public static string CreateTempCopy(string cpkPath)
	{
		string temp = Path.Combine(Path.GetTempPath(), "cpkmakec_ffbejp_" + Guid.NewGuid().ToString("N") + ".cpk");
		File.Copy(cpkPath, temp, overwrite: true);
		AppDomain.CurrentDomain.ProcessExit += delegate
		{
			DeleteTemp(temp);
		};
		return temp;
	}

	public static void DeleteTemp(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch (Exception)
		{
		}
	}
}
