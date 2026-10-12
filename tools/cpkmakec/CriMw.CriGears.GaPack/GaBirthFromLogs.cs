using System;

namespace CriMw.CriGears.GaPack;

public class GaBirthFromLogs : IGaBirth
{
	private Random m_randomfarm;

	private Logs m_logs;

	public GaBirthFromLogs(Logs logs, Random randomfarm)
	{
		m_logs = logs;
		m_randomfarm = randomfarm;
	}

	public GaIndividual Birth()
	{
		int num = 0;
		GaIndividualEvalMem gaIndividualEvalMem = new GaIndividualEvalMem(m_randomfarm);
		int size = m_logs.GetSize();
		gaIndividualEvalMem.m_dna = new int[size];
		if (num == 0)
		{
			for (int i = 0; i < size; i++)
			{
				gaIndividualEvalMem.m_dna[i] = m_logs.GetFileId(i);
			}
		}
		else
		{
			for (int i = 0; i < size; i++)
			{
				gaIndividualEvalMem.m_dna[i] = m_randomfarm.Next();
			}
		}
		return gaIndividualEvalMem;
	}

	public GaIndividual Birth(GaIndividual a, GaIndividual b)
	{
		return new GaIndividualEvalMem(a, b);
	}
}
