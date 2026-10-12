using System.Collections.Generic;

namespace cpkmakecCs;

internal class FileInfoSortIndexSorter : IComparer<CMkFileInfo>
{
	public int Compare(CMkFileInfo a, CMkFileInfo b)
	{
		if (a == b)
		{
			return 0;
		}
		return (int)((long)a.SortIndex - (long)b.SortIndex);
	}
}
