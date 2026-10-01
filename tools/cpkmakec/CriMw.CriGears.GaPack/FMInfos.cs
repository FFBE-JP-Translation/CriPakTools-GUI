using System;

namespace CriMw.CriGears.GaPack;

public class FMInfos
{
	public class FMInfo
	{
		public string m_filename;

		public long m_filesize;

		public FMInfo(string filename, long size)
		{
			m_filename = filename;
			m_filesize = size;
		}

		public void Disp()
		{
			Console.Write("{0:s} : {1:d}\n", m_filename, m_filesize);
		}

		public long GetFileSize()
		{
			return m_filesize;
		}
	}

	public FMInfo[] m_fminfos;

	private int m_listsize;

	public void Disp()
	{
		FMInfo[] fminfos = m_fminfos;
		foreach (FMInfo fMInfo in fminfos)
		{
			fMInfo.Disp();
		}
	}

	public FMInfos(int size)
	{
		m_listsize = size;
		m_fminfos = new FMInfo[m_listsize];
	}

	public Infos FMInfos2Infos(FileidMaster fim)
	{
		Infos infos = new Infos();
		infos.m_fileidmax = m_listsize;
		infos.m_infos = new Infos.Info[m_fminfos.Length];
		for (int i = 0; i < m_fminfos.Length; i++)
		{
			infos.m_infos[i] = new Infos.Info(fim.GetId(m_fminfos[i].m_filename), m_fminfos[i].m_filesize);
		}
		return infos;
	}

	public Fseq FMInfos2Fseq(FileidMaster fim)
	{
		Fseq fseq = new Fseq();
		fseq.m_fileids = new int[m_fminfos.Length];
		for (int i = 0; i < m_fminfos.Length; i++)
		{
			fseq.m_fileids[i] = fim.GetId(m_fminfos[i].m_filename);
		}
		return fseq;
	}
}
