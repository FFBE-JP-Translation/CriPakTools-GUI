using System.Collections.Generic;
using System.IO;
using System.Text;

namespace cpkmakecCs;

public class DebugUtilities
{
	public enum EnumInputType
	{
		OutCsv,
		LogFile
	}

	public static bool CreateDummyFilesByTextFile(string logfilename, EnumInputType type)
	{
		if (!File.Exists(logfilename))
		{
			return false;
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		using (StreamReader streamReader = new StreamReader(logfilename, Encoding.GetEncoding("shift_jis")))
		{
			do
			{
				bool flag = true;
				if (ExecLine(streamReader, type, out var fname, out var fsize) && !dictionary.ContainsKey(fname))
				{
					dictionary.Add(fname, fsize);
				}
			}
			while (!streamReader.EndOfStream);
		}
		return true;
	}

	private static bool ExecLine(StreamReader reader, EnumInputType type, out string fname, out int fsize)
	{
		fname = null;
		fsize = 0;
		string text = reader.ReadLine();
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		string[] array = text.Split(new char[1] { ',' });
		switch (type)
		{
		case EnumInputType.LogFile:
			if (array.Length < 10 || array.Length != 10 || array[0] != "#CRIFS" || array[3] != "Load")
			{
				return false;
			}
			fname = array[4];
			int.TryParse(array[6], out fsize);
			return true;
		case EnumInputType.OutCsv:
			if (array.Length > 3)
			{
				fname = array[0];
				CreateDummyFile(fname, 1234);
			}
			break;
		}
		return true;
	}

	public static void CreateDummyFile(string fname, int fsize)
	{
		string directoryName = Path.GetDirectoryName(fname);
		if (!Directory.Exists(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		using BinaryWriter binaryWriter = new BinaryWriter(File.Open(fname, FileMode.Create));
		for (int i = 0; i < fsize; i++)
		{
			binaryWriter.Write((byte)(i % 256));
		}
	}
}
