using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace cpkmakecCs;

/// <summary>
/// FFBE JP CPK format: every file stored in the CPK is encrypted with a key derived from its own file name.
/// Packing encrypts the local files (into temporary copies) before they are handed to CpkMaker,
/// extracting decrypts the extracted files afterwards.
///
///   hash  : h = 0; for each byte c of the ASCII file name: h = h * 31 + c   (uint32)
///   ks(i) : ((h >> (8 * (i & 3))) & 0xFF) + (i % 255)                    (byte)
///   half = size / 2, second = half + (size & 1)
///   decrypt, for i in [0, half):  a = d[i]; b = d[second + i]; d[i] = ks ^ b; d[second + i] = a ^ ~ks
///   encrypt is the inverse of that.
/// </summary>
public static class FfbeJp
{
	private static string tempDir;

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

	/// <summary>
	/// Writes an encrypted copy of a local file into a temporary directory and returns its path.
	/// The time stamp of the source file is kept.
	/// </summary>
	public static string EncryptToTemp(string localPath, string contentName, uint index)
	{
		if (tempDir == null)
		{
			tempDir = Path.Combine(Path.GetTempPath(), "cpkmakec_ffbejp_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDir);
			AppDomain.CurrentDomain.ProcessExit += delegate
			{
				Cleanup();
			};
		}
		string dir = Path.Combine(tempDir, index.ToString());
		Directory.CreateDirectory(dir);
		string dest = Path.Combine(dir, KeyName(localPath));
		File.WriteAllBytes(dest, Encrypt(File.ReadAllBytes(localPath), KeyName(contentName)));
		File.SetLastWriteTime(dest, File.GetLastWriteTime(localPath));
		return dest;
	}

	/// <summary>Decrypts every file below the directory in place. Returns the number of files.</summary>
	public static int DecryptDirectory(string dir)
	{
		int count = 0;
		if (!Directory.Exists(dir))
		{
			return 0;
		}
		foreach (string file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
		{
			string name = KeyName(file);
			if (!AppliesTo(name))
			{
				continue;
			}
			byte[] data = Decrypt(File.ReadAllBytes(file), name);
			if (data.Length >= 8 && Encoding.ASCII.GetString(data, 0, 8) == "CRILAYLA")
			{
				Console.WriteLine("Warning: " + name + " is CRILAYLA compressed, it is decrypted but still compressed.");
			}
			DateTime time = File.GetLastWriteTime(file);
			File.WriteAllBytes(file, data);
			File.SetLastWriteTime(file, time);
			count++;
		}
		return count;
	}

	public static void Cleanup()
	{
		if (tempDir != null)
		{
			try
			{
				Directory.Delete(tempDir, recursive: true);
			}
			catch (Exception)
			{
			}
			tempDir = null;
		}
	}
}
