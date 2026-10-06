using System;
using System.IO;

namespace cpkmakecCs;

public class CMkFileInfo : CMkInfoBase
{
	public string LocalFilePath;

	public string ContentFilePath;

	public uint FileId;

	public bool Compress;

	public bool CompressAdd;

	public string Attribute;

	public string Groups;

	public DateTime FileDateTime;

	public object Tag;

	public long OriginalSize;

	public long CompressedSize;

	public uint SortIndex;

	public uint RegisterIndex;

	public bool IsSameTime(DateTime dt)
	{
		if (FileDateTime.Second == dt.Second && FileDateTime.Day == dt.Day && FileDateTime.Month == dt.Month && FileDateTime.Year == dt.Year && FileDateTime.Hour == dt.Hour && FileDateTime.Minute == dt.Minute)
		{
			return true;
		}
		return false;
	}

	public bool IsFileExist()
	{
		if (File.Exists(LocalFilePath))
		{
			FileInfo fileInfo = new FileInfo(LocalFilePath);
			FileDateTime = fileInfo.LastWriteTime;
			OriginalSize = fileInfo.Length;
			return true;
		}
		return false;
	}
}
