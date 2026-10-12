using System;

namespace CriMw.CriGears.GaPack;

internal class CalcSeekamount
{
	private struct SadrEadr
	{
		public long m_sadr;

		public long m_eadr;

		public SadrEadr(long sadr, long eadr)
		{
			m_sadr = sadr;
			m_eadr = eadr;
		}
	}

	private const int c_penalty = 1000;

	private SadrEadr[] m_sadreadrs;

	private void sadreadrset(Infos infos, Fseq fseq)
	{
		long num = 0L;
		for (int i = 0; i < fseq.m_fileids.Length; i++)
		{
			int num2 = fseq.m_fileids[i];
			long num3 = num + infos.GetFileSize(num2);
			ref SadrEadr reference = ref m_sadreadrs[num2];
			reference = new SadrEadr(num, num3);
			num = num3;
		}
	}

	public CalcSeekamount(Infos infos, Fseq fseq)
	{
		m_sadreadrs = new SadrEadr[infos.m_fileidmax];
		sadreadrset(infos, fseq);
	}

	public long Calc(Logs logs)
	{
		long num = 0L;
		long num2 = 0L;
		Logs.Log[] logs2 = logs.m_logs;
		foreach (Logs.Log log in logs2)
		{
			int id = log.m_id;
			long num3 = Math.Abs(m_sadreadrs[id].m_sadr + log.m_offset - num);
			num3 = ((num3 != 0) ? (num3 + 1000) : num3);
			num2 += num3;
			num = m_sadreadrs[id].m_sadr + log.m_size;
		}
		return num2;
	}
}
