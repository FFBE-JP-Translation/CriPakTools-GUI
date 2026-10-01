using System;

namespace CriMw.CriGears.GaPack;

public class LogsInfosGaFarm : GaFarm
{
	public class Ga2Fseq : Fseq
	{
		public int m_dd;

		public Ga2Fseq(int MAX, GaIndividual gi)
		{
			m_dd = 0;
			m_fileids = new int[MAX];
			int[] array = new int[MAX];
			for (int i = 0; i < MAX; i++)
			{
				array[i] = 0;
			}
			int num = 0;
			int[] dna = gi.GetDna();
			for (int j = 0; j < dna.Length; j++)
			{
				int num2 = dna[j] % MAX;
				if (num2 < MAX && array[num2] == 0)
				{
					array[num2] = 1;
					m_fileids[num] = num2;
					num++;
					if (num >= MAX)
					{
						break;
					}
				}
			}
			m_dd = num;
			if (num >= MAX)
			{
				return;
			}
			for (int i = 0; i < MAX; i++)
			{
				if (array[i] == 0)
				{
					m_fileids[num] = i;
					num++;
				}
			}
		}
	}

	private const int c_penalty = 100;

	private NewRecordEvalCardFseq m_nr;

	private Logs m_logs;

	private Infos m_infos;

	private int m_mcnt;

	protected EvalCardFseq m_minimum;

	public void Exec(NewRecordEvalCardFseq nr)
	{
		m_nr = nr;
		Exec();
	}

	public int GetMCnt()
	{
		return m_mcnt;
	}

	public LogsInfosGaFarm(IGaBirth igb, Logs logs, Infos infos, Random farmrondom)
		: base(igb, farmrondom)
	{
		m_minimum.m_point64 = long.MaxValue;
		m_logs = logs;
		m_infos = infos;
		m_mcnt = 0;
	}

	private GaIndividualEvalMem.EvalCard EvalGiPure(GaIndividual gi)
	{
		GaIndividualEvalMem.EvalCard result = default(GaIndividualEvalMem.EvalCard);
		Ga2Fseq ga2Fseq = new Ga2Fseq(m_infos.m_fileidmax, gi);
		CalcSeekamount calcSeekamount = new CalcSeekamount(m_infos, ga2Fseq);
		result.m_dd = ga2Fseq.m_dd;
		result.m_point64 = calcSeekamount.Calc(m_logs);
		result.m_dnalength = gi.GetDna().Length;
		if (result.m_point64 < m_minimum.m_point64)
		{
			m_minimum.m_dd = result.m_dd;
			m_minimum.m_point64 = result.m_point64;
			m_minimum.m_fseq = ga2Fseq;
			m_mcnt++;
			m_nr.NewRecord(m_minimum);
		}
		return result;
	}

	public GaIndividualEvalMem.EvalCard EvalGi(GaIndividual gi)
	{
		GaIndividualEvalMem gaIndividualEvalMem = (GaIndividualEvalMem)gi;
		if (gaIndividualEvalMem.m_evalcard.m_point64 >= 0)
		{
			return gaIndividualEvalMem.m_evalcard;
		}
		return gaIndividualEvalMem.m_evalcard = EvalGiPure(gi);
	}

	public override WINPUSH Battle(GaIndividual a, GaIndividual b)
	{
		GaIndividualEvalMem.EvalCard evalCard = EvalGi(a);
		GaIndividualEvalMem.EvalCard evalCard2 = EvalGi(b);
		if (evalCard.m_point64 < evalCard2.m_point64)
		{
			return WINPUSH.AWIN;
		}
		if (evalCard.m_point64 > evalCard2.m_point64)
		{
			return WINPUSH.BWIN;
		}
		if (evalCard.m_dnalength < evalCard2.m_dnalength)
		{
			return WINPUSH.AWIN;
		}
		if (evalCard.m_dnalength > evalCard2.m_dnalength)
		{
			return WINPUSH.BWIN;
		}
		return WINPUSH.PUSH;
	}

	public void Main(int loopmax)
	{
		for (int i = 0; i < loopmax; i++)
		{
			Exec();
			if (i % 10000 == 0)
			{
				Console.Write("{0:d}万回\n", i / 10000);
				m_minimum.m_fseq.Disp();
			}
		}
		Console.Write("最後\n");
		m_minimum.m_fseq.Disp();
	}
}
