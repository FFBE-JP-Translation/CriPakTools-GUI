using System;

namespace CriMw.CriGears.GaPack;

public class GaFarm
{
	public enum WINPUSH
	{
		AWIN = 1,
		BWIN = -1,
		PUSH = 0
	}

	private const int c_farmsize = 500;

	private IGaBirth m_birth;

	private int m_rotation;

	private Random m_farmrandom;

	protected GaIndividual[] m_gi;

	public void DispGi(int ind)
	{
		Console.Write("{0:d} ", ind);
		m_gi[ind].Disp();
	}

	public virtual WINPUSH Battle(GaIndividual a, GaIndividual b)
	{
		throw new Exception("Battle is not implemented.");
	}

	public void Exec()
	{
		int rotation = m_rotation;
		m_rotation = (m_rotation + 1) % 500;
		int num = m_farmrandom.Next(499);
		if (num >= rotation)
		{
			num++;
		}
		WINPUSH wINPUSH = Battle(m_gi[rotation], m_gi[num]);
		int num2 = ((wINPUSH != WINPUSH.BWIN) ? rotation : num);
		int num3 = ((wINPUSH != WINPUSH.BWIN) ? num : rotation);
		int num4 = num3;
		m_gi[num4] = m_birth.Birth(m_gi[num2], m_gi[num3]);
	}

	public GaFarm(IGaBirth birth, Random farmrandom)
	{
		m_birth = birth;
		m_rotation = 0;
		m_farmrandom = farmrandom;
		m_gi = new GaIndividual[500];
		for (int i = 0; i < 500; i++)
		{
			m_gi[i] = m_birth.Birth();
		}
	}

	public void Disp()
	{
		GaIndividual[] gi = m_gi;
		foreach (GaIndividual gaIndividual in gi)
		{
			gaIndividual.Disp();
		}
	}
}
