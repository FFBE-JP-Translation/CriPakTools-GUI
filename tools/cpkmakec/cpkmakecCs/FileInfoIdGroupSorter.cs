using System.Collections.Generic;

namespace cpkmakecCs;

internal class FileInfoIdGroupSorter : IComparer<CMkFileInfo>
{
	public int Compare(CMkFileInfo a, CMkFileInfo b)
	{
		if (a == b)
		{
			return 0;
		}
		int num = string.Compare(a.Groups, b.Groups);
		if (num == 0)
		{
			num = string.Compare(a.Attribute, b.Attribute);
			if (num == 0)
			{
				num = string.Compare(a.ContentFilePath, b.ContentFilePath);
				if (num == 0)
				{
					num = (int)(a.FileId - b.FileId);
				}
				return num;
			}
			return num;
		}
		return num;
	}
}
