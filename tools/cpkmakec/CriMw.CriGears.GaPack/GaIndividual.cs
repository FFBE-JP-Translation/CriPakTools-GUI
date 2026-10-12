using System;

namespace CriMw.CriGears.GaPack;

public class GaIndividual
{
	private const double c_mutationrate = 0.01;

	private const int c_targetfilenum = 10000;

	private const int c_maxdnalength = 20000;

	private Random m_farmrandom;

	public int[] m_dna;

	public int[] GetDna()
	{
		return m_dna;
	}

	public void Disp()
	{
		int[] dna = m_dna;
		foreach (int num in dna)
		{
			Console.Write("{0:d} ", num);
		}
		Console.Write("\n");
	}

	public GaIndividual(Random farmrandom)
	{
		m_farmrandom = farmrandom;
	}

	public GaIndividual(int length, Random farmrandom)
		: this(farmrandom)
	{
		m_dna = new int[length];
		for (int i = 0; i < length; i++)
		{
			m_dna[i] = farmrandom.Next(int.MaxValue);
		}
	}

	public GaIndividual(GaIndividual a, GaIndividual b)
	{
		m_farmrandom = b.m_farmrandom;
		int num = m_farmrandom.Next(a.m_dna.Length + 1);
		int num2 = m_farmrandom.Next(b.m_dna.Length + 1);
		if (num > 20000)
		{
			num = 20000;
			num2 = 0;
		}
		else if (num + num2 > 20000)
		{
			num2 = 20000 - num;
		}
		int num3 = num + num2;
		m_dna = new int[num3];
		int num4 = 0;
		for (int i = 0; i < num; i++)
		{
			m_dna[num4] = a.m_dna[i];
			num4++;
		}
		for (int i = 0; i < num2; i++)
		{
			m_dna[num4] = b.m_dna[b.m_dna.Length - num2 + i];
			num4++;
		}
		if (m_dna.Length > 0 && m_farmrandom.NextDouble() < 0.01)
		{
			m_dna[m_farmrandom.Next(m_dna.Length)] = m_farmrandom.Next(int.MaxValue);
		}
	}
}
