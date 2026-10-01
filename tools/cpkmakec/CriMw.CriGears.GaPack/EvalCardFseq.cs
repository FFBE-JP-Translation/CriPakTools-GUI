using System;

namespace CriMw.CriGears.GaPack;

public struct EvalCardFseq
{
	public int m_dd;

	public long m_point64;

	public Fseq m_fseq;

	public void Disp()
	{
		Console.Write("Seek評価値={0:d}\n", m_point64);
		m_fseq.Disp();
	}
}
