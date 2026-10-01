using System;
using System.Collections.Generic;

namespace cpkmakecCs;

internal class FileInfoFileGroupRegistSorter : IComparer<CMkFileInfo>
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
				num = Convert.ToInt32(a.Compress) - Convert.ToInt32(b.Compress);
				if (num == 0)
				{
					num = (int)((long)a.RegisterIndex - (long)b.RegisterIndex);
				}
			}
		}
		return num;
	}
}
