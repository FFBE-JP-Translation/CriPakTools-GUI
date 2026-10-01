using System;

namespace CriMw.CriGears.GaPack;

public class GaBirthRand : IGaBirth
{
	private Random m_farmrandom;

	private int m_length;

	public GaBirthRand(int length, Random farmrandom)
	{
		m_farmrandom = farmrandom;
		m_length = length;
	}

	GaIndividual IGaBirth.Birth()
	{
		return new GaIndividual(m_length, m_farmrandom);
	}

	GaIndividual IGaBirth.Birth(GaIndividual papa, GaIndividual mama)
	{
		return new GaIndividual(papa, mama);
	}
}
