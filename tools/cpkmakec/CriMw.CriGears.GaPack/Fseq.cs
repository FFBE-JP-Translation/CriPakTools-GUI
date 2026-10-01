using System;

namespace CriMw.CriGears.GaPack;

public class Fseq
{
	public int[] m_fileids;

	public int GetFileid(int ind)
	{
		return m_fileids[ind];
	}

	public int GetSize()
	{
		return m_fileids.Length;
	}

	public void Disp()
	{
		for (int i = 0; i < m_fileids.Length; i++)
		{
			Console.Write("{0:d3} ", m_fileids[i]);
			if (i % 10 == 9)
			{
				Console.Write("\n");
			}
		}
		Console.Write("\n");
	}
}
