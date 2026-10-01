using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace cpkmakecCs;

public class CAnalyCsvFile : IDisposable
{
	public enum TextCode
	{
		CODE_SJIS,
		CODE_UTF8,
		CODE_EUC
	}

	private StreamReader m_reader;

	private long m_current_id;

	private uint m_read_line;

	private string m_filename;

	private Encoding m_encoding;

	private Hashtable m_hashtable;

	private bool EnableIdChecker;

	private bool disposed = false;

	private string[] TargetGroupList = null;

	public CAnalyCsvFile()
	{
		m_hashtable = new Hashtable();
		resetParams();
		m_encoding = Encoding.GetEncoding("shift_jis");
	}

	~CAnalyCsvFile()
	{
	}

	public void Dispose()
	{
		if (!disposed && m_reader != null)
		{
			m_reader.Close();
			m_reader.Dispose();
			m_reader = null;
		}
		disposed = true;
	}

	private void resetParams()
	{
		m_reader = null;
		m_read_line = 0u;
		m_current_id = -1L;
		m_hashtable.Clear();
		EnableIdChecker = true;
	}

	private int getNumIncludedStr(string targ_str, string in_str)
	{
		int num = 0;
		int startIndex = 0;
		while (true)
		{
			bool flag = true;
			startIndex = targ_str.IndexOf(in_str, startIndex);
			if (startIndex < 0)
			{
				break;
			}
			startIndex++;
			num++;
		}
		return num;
	}

	private string replaceCommaToAstaInDoubleQuote(string targ_str)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		foreach (char c in targ_str)
		{
			if (c == '"')
			{
				num++;
			}
			if (num % 2 == 1 && c == ',')
			{
				stringBuilder.Append("*");
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}

	private string[] GetSeparatedString(string str)
	{
		int numIncludedStr = getNumIncludedStr(str, "\"");
		if (numIncludedStr % 2 != 0)
		{
			throw new Exception(GetFilenameAndLineString() + "Invalid double quote.");
		}
		str = replaceCommaToAstaInDoubleQuote(str);
		char[] separator = new char[1] { ',' };
		string[] array = str.Split(separator);
		char[] trimChars = new char[3] { ' ', '\t', '"' };
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = array[i].Trim(trimChars).Replace("*", ",");
		}
		return array;
	}

	private void SetCode(TextCode code)
	{
		switch (code)
		{
		case TextCode.CODE_SJIS:
			m_encoding = Encoding.GetEncoding("shift_jis");
			break;
		case TextCode.CODE_UTF8:
			m_encoding = Encoding.GetEncoding("utf-8");
			break;
		case TextCode.CODE_EUC:
			m_encoding = Encoding.GetEncoding("euc-jp");
			break;
		}
	}

	public static string GetCodeString(TextCode code)
	{
		return code switch
		{
			TextCode.CODE_SJIS => "SJIS", 
			TextCode.CODE_UTF8 => "UTF-8", 
			TextCode.CODE_EUC => "EUC", 
			_ => "Unknown", 
		};
	}

	public bool AnalizeCsvFile(string csvfname, TextCode code)
	{
		resetParams();
		SetCode(code);
		if (File.Exists(csvfname))
		{
			m_reader = new StreamReader(csvfname, m_encoding);
			disposed = false;
			m_filename = csvfname;
			return true;
		}
		throw new Exception(GetFilenameString() + " is not found.");
	}

	private bool IsNumberString(string txt)
	{
		uint result;
		return uint.TryParse(txt, out result);
	}

	public bool GetLine(CMkFileInfo inf)
	{
		inf.Groups = "";
		inf.Attribute = "";
		while (true)
		{
			bool flag = true;
			string text = m_reader.ReadLine();
			m_read_line++;
			inf.Lines = m_read_line;
			if (text == null)
			{
				return false;
			}
			if (text.Length <= 0 || text[0] == ':' || text[0] == '#')
			{
				continue;
			}
			string[] separatedString = GetSeparatedString(text);
			if (separatedString != null)
			{
				int num = 0;
				inf.LocalFilePath = separatedString[num];
				num++;
				if (separatedString.Length == 1)
				{
					inf.FileId = (uint)(m_current_id + 1);
					inf.ContentFilePath = Path.GetFileName(separatedString[0]);
					inf.Compress = false;
					inf.CompressAdd = false;
					m_current_id++;
					break;
				}
				if (separatedString.Length == 2 && !IsNumberString(separatedString[1]))
				{
					separatedString = GetSeparatedString(text + ",,");
				}
				if (separatedString.Length == 3)
				{
					if (IsNumberString(separatedString[2]))
					{
						separatedString = GetSeparatedString(text + ",");
					}
					else
					{
						inf.ContentFilePath = Path.GetFileName(separatedString[0]);
					}
				}
				if (separatedString.Length >= 4)
				{
					if (separatedString[num].Length != 0)
					{
						inf.ContentFilePath = separatedString[num];
					}
					else
					{
						inf.ContentFilePath = Path.GetFileName(separatedString[0]);
					}
					num++;
				}
				if (separatedString[num].Length != 0 && IsNumberString(separatedString[num]))
				{
					uint num2 = uint.Parse(separatedString[num]);
					if (EnableIdChecker && m_hashtable.ContainsKey(num2))
					{
						throw new Exception(GetFilenameAndLineString() + " Invalid number of file ID.");
					}
					m_hashtable.Add(num2, null);
					inf.FileId = num2;
				}
				else
				{
					inf.FileId = (uint)(m_current_id + 1);
				}
				num++;
				if (separatedString.Length > num)
				{
					inf.Compress = ConvToBoolean(separatedString[num]);
					inf.CompressAdd = GetCompressAddFlag(separatedString[num]);
					num++;
				}
				if (separatedString.Length >= 5)
				{
					if (TargetGroupList == null)
					{
						inf.Groups = separatedString[num];
					}
					else
					{
						string targetGroup = GetTargetGroup(separatedString[num]);
						if (targetGroup == null)
						{
							continue;
						}
						inf.Groups = targetGroup;
					}
					num++;
				}
				if (separatedString.Length >= 6)
				{
					inf.Attribute = separatedString[num];
					num++;
				}
				m_current_id = inf.FileId;
				break;
			}
			inf.LocalFilePath = "";
			inf.FileId = 0u;
			inf.Compress = false;
			inf.CompressAdd = false;
		}
		return true;
	}

	public bool GetLine(CMkAttrInfo inf)
	{
		string[] separatedString;
		while (true)
		{
			bool flag = true;
			string text = m_reader.ReadLine();
			m_read_line++;
			inf.Lines = m_read_line;
			if (text == null)
			{
				return false;
			}
			if (text.Length > 0 && text[0] != ':' && text[0] != '#')
			{
				separatedString = GetSeparatedString(text);
				if (separatedString != null && separatedString.Length >= 2)
				{
					break;
				}
			}
		}
		inf.AttributeName = separatedString[0];
		if (!int.TryParse(separatedString[1], out inf.Alignment))
		{
			return false;
		}
		return true;
	}

	private string GetTargetGroup(string groups)
	{
		string[] list = GetList(groups);
		List<string> list2 = new List<string>();
		string[] targetGroupList = TargetGroupList;
		foreach (string text in targetGroupList)
		{
			string[] array = list;
			foreach (string text2 in array)
			{
				if (text2.ToUpper().StartsWith(text.ToUpper()))
				{
					list2.Add(text2);
				}
			}
		}
		if (list2.Count <= 0)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int k = 0; k < list2.Count; k++)
		{
			stringBuilder.Append(list2[k]);
			stringBuilder.Append(",");
		}
		return stringBuilder.ToString().Trim(new char[1] { ',' });
	}

	private string[] GetList(string str)
	{
		string[] array = str.Split(new char[1] { ',' });
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = array[i].Trim();
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (!array[i].StartsWith("/"))
			{
				array[i] = "/" + array[i];
			}
			if (!array[i].EndsWith("/"))
			{
				array[i] += "/";
			}
		}
		return array;
	}

	public void SetTargetGroup(string str)
	{
		if (str.ToLower() == "(none)")
		{
			TargetGroupList = new string[1] { "" };
		}
		else
		{
			TargetGroupList = GetList(str);
		}
	}

	private bool ConvToBoolean(string str)
	{
		string text = str.ToUpper();
		if (str.Length == 0)
		{
			return false;
		}
		if (text.Equals("COMPRESSADD"))
		{
			return true;
		}
		if (text.Equals("COMPRESS"))
		{
			return true;
		}
		if (text.Equals("COMPRESSION"))
		{
			return true;
		}
		if (text.Equals("C"))
		{
			return true;
		}
		if (text.Equals("UNCOMPRESS"))
		{
			return false;
		}
		if (text.Equals("UNCOMPRESSION"))
		{
			return false;
		}
		if (text.Equals("UC"))
		{
			return false;
		}
		throw new Exception(GetFilenameAndLineString() + " Unknown compression word.");
	}

	private bool GetCompressAddFlag(string str)
	{
		string text = str.ToUpper();
		if (str.Length == 0)
		{
			return false;
		}
		if (text.Equals("COMPRESSADD"))
		{
			return true;
		}
		if (text.Equals("COMPRESS"))
		{
			return false;
		}
		if (text.Equals("COMPRESSION"))
		{
			return false;
		}
		if (text.Equals("C"))
		{
			return false;
		}
		if (text.Equals("UNCOMPRESS"))
		{
			return false;
		}
		if (text.Equals("UNCOMPRESSION"))
		{
			return false;
		}
		if (text.Equals("UC"))
		{
			return false;
		}
		throw new Exception(GetFilenameAndLineString() + " Unknown compression word.");
	}

	private string GetFilenameString()
	{
		return "\"" + m_filename + "\"";
	}

	private string GetLineString()
	{
		return "line." + m_read_line;
	}

	private string GetFilenameAndLineString()
	{
		return GetLineString() + " " + GetFilenameString();
	}
}
