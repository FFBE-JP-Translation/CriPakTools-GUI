using System;
using System.IO;
using System.Text;

namespace CriMw.CriGears.GaPack;

public class FMLogReader : IFMLogReader
{
	public virtual void Load(string crifs, string timeptr, string pristr, string cmdstr, string filename, string adrstr, string sizestr, string cpkname, string offsetstr, string did)
	{
	}

	private static string[] Parse(string linstr)
	{
		string[] array = linstr.Split(new char[1] { ',' });
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = array[i].Trim();
		}
		return array;
	}

	public void Read(string filename)
	{
		Read(filename, Encoding.GetEncoding("shift_jis"));
	}

	public void Read(string filename, Encoding encode)
	{
		if (!File.Exists(filename))
		{
			throw new FileNotFoundException();
		}
		using StreamReader streamReader = new StreamReader(filename, encode);
		int num = 1;
		try
		{
			while (!streamReader.EndOfStream)
			{
				string[] array = Parse(streamReader.ReadLine());
				if (array[0] == "#CRIFS" && array[3] == "Load")
				{
					if (array.Length != 10 && array.Length != 17)
					{
						string text = array.Length.ToString();
					}
					Load(array[0], array[1], array[2], array[3], array[4], array[5], array[6], array[7], array[8], array[9]);
				}
				num++;
			}
		}
		catch (Exception ex)
		{
			throw new Exception("\"" + filename + "\" : Invalid line in " + num + ". " + ex.Message);
		}
	}
}
