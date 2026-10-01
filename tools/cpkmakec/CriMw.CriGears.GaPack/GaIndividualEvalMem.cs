using System;

namespace CriMw.CriGears.GaPack;

public class GaIndividualEvalMem : GaIndividual
{
	public struct EvalCard
	{
		public long m_point64;

		public int m_dd;

		public int m_dnalength;

		public void Disp()
		{
			Console.Write("{0:d} {1:d} {2:d}\n", m_dd, m_point64, m_dnalength);
		}
	}

	public EvalCard m_evalcard;

	public GaIndividualEvalMem(Random farmrandom)
		: base(farmrandom)
	{
		m_evalcard.m_point64 = -1L;
	}

	public GaIndividualEvalMem(int initlength, Random farmrandom)
		: base(initlength, farmrandom)
	{
		m_evalcard.m_point64 = -1L;
	}

	public GaIndividualEvalMem(GaIndividual papa, GaIndividual mama)
		: base(papa, mama)
	{
		m_evalcard.m_point64 = -1L;
	}
}
