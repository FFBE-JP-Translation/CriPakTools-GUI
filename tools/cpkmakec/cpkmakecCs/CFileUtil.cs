using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace cpkmakecCs;

internal class CFileUtil
{
	public static string ChangeExtention(string fname, string newext)
	{
		string directoryName = Path.GetDirectoryName(fname);
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fname);
		return Path.Combine(directoryName, fileNameWithoutExtension + newext);
	}

	public static string AddExtention(string fname, string newext)
	{
		string directoryName = Path.GetDirectoryName(fname);
		string extension = Path.GetExtension(fname);
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fname);
		if (extension != "")
		{
			return fname;
		}
		return Path.Combine(directoryName, fileNameWithoutExtension + newext);
	}

	public static bool IsMatchExtention(string fname, string ext)
	{
		try
		{
			string text = Path.GetExtension(fname).ToUpper();
			ext = ext.ToUpper();
			if (text == ext)
			{
				return true;
			}
		}
		catch (Exception)
		{
			return false;
		}
		return false;
	}

	public static bool IsMatchFileFourCC(string fname, string fourcc)
	{
		if (!File.Exists(fname))
		{
			return false;
		}
		try
		{
			int length = fourcc.Length;
			using FileStream fileStream = new FileStream(fname, FileMode.Open, FileAccess.Read);
			if (fileStream.Length < length)
			{
				return false;
			}
			for (int i = 0; i < length; i++)
			{
				int num = fileStream.ReadByte();
				int num2 = fourcc[i];
				if (num != num2)
				{
					return false;
				}
			}
		}
		catch (Exception)
		{
			return false;
		}
		return true;
	}

	public static string DeleteDoubleQuote(string fname)
	{
		if (fname.StartsWith("\""))
		{
			fname = fname.Substring(1);
		}
		if (fname.EndsWith("\""))
		{
			fname = fname.Substring(0, fname.Length - 1);
		}
		return fname;
	}

	public static string AddDoubleQuote(string fname)
	{
		return "\"" + fname + "\"";
	}

	public static bool IsMatchByWildcard(string testString, string wildcardString)
	{
		string text = wildcardString;
		bool flag = true;
		if (string.IsNullOrEmpty(wildcardString))
		{
			return true;
		}
		text = text.Trim(';', '|', ' ', '"');
		text = text.Replace(".", "\\.");
		text = text.Replace("?", ".");
		text = text.Replace("*", ".*");
		text = text.Replace(";", "$|^");
		text = "^" + text + "$";
		Regex regex = new Regex(text, RegexOptions.IgnoreCase | RegexOptions.Singleline);
		try
		{
			flag = regex.IsMatch(testString.ToUpper());
		}
		catch (Exception)
		{
			flag = false;
		}
		return flag;
	}

	public static string GetApplicationVersionFromResource()
	{
		FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
		string fileVersion = versionInfo.FileVersion;
		fileVersion = fileVersion.Replace(" ", "");
		char[] separator = new char[1] { '.' };
		string[] array = fileVersion.Split(separator);
		for (int i = 1; i < array.Length; i++)
		{
			if (array[i].Length == 1)
			{
				array[i] = "0" + array[i];
			}
		}
		return $"{array[0]}.{array[1]}.{array[2]}";
	}

	public static string GetMyFileVersionString()
	{
		FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
		StringBuilder stringBuilder = new StringBuilder();
		string value = versionInfo.FileMajorPart.ToString();
		stringBuilder.Append("Ver.");
		stringBuilder.Append(value);
		stringBuilder.Append(".");
		string value2 = versionInfo.FileMinorPart.ToString();
		if (versionInfo.FileMinorPart < 10)
		{
			stringBuilder.Append("0");
		}
		stringBuilder.Append(value2);
		stringBuilder.Append(".");
		string value3 = versionInfo.FileBuildPart.ToString();
		if (versionInfo.FileBuildPart < 10)
		{
			stringBuilder.Append("0");
		}
		stringBuilder.Append(value3);
		return stringBuilder.ToString();
	}

	private static FileVersionInfo GetFileVersionInfo()
	{
		return FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
	}

	public static string GetMyProductString()
	{
		return GetFileVersionInfo().ProductName;
	}

	public static string GetMyCopyrightString()
	{
		return GetFileVersionInfo().LegalCopyright;
	}

	public static string GetMyFileVersionWithProductString()
	{
		return GetMyProductString() + " " + GetMyFileVersionString();
	}
}
