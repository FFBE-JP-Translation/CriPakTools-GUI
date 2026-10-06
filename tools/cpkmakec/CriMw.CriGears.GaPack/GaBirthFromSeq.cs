using System;

namespace CriMw.CriGears.GaPack;

public class GaBirthFromSeq : IGaBirth
{
	private Random m_randomfarm;

	private Fseq m_fseq;

	public GaBirthFromSeq(Fseq fseq, Random randomfarm)
	{
		m_fseq = fseq;
		m_randomfarm = randomfarm;
	}

	public GaIndividual Birth()
	{
		GaIndividualEvalMem gaIndividualEvalMem = new GaIndividualEvalMem(m_randomfarm);
		int size = m_fseq.GetSize();
		gaIndividualEvalMem.m_dna = new int[size];
		for (int i = 0; i < size; i++)
		{
			gaIndividualEvalMem.m_dna[i] = m_fseq.GetFileid(i);
		}
		return gaIndividualEvalMem;
	}

	public GaIndividual Birth(GaIndividual papa, GaIndividual mama)
	{
		return new GaIndividualEvalMem(papa, mama);
	}
}
