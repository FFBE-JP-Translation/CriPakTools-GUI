using System;
using System.Collections;

namespace CriMw.CriGears.GaPack;

public class FileidMaster
{
	private Hashtable m_s2n;

	private ArrayList m_n2s;

	public string GetFilename(int id)
	{
		return (string)m_n2s[id];
	}

	public int GetCount()
	{
		return m_n2s.Count;
	}

	public void Disp()
	{
		for (int i = 0; i < m_n2s.Count; i++)
		{
			Console.Write("{0:d}:{1:s}\n", i, m_n2s[i]);
		}
	}

	public void AddAll(FMLog log)
	{
		foreach (FMLog.ST_L item in log.m_log)
		{
			AddReport(item.m_filename);
		}
	}

	public void AddAll(FMInfos Infos)
	{
		FMInfos.FMInfo[] fminfos = Infos.m_fminfos;
		foreach (FMInfos.FMInfo fMInfo in fminfos)
		{
			AddReport(fMInfo.m_filename);
		}
	}

	public FileidMaster()
	{
		m_s2n = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
		m_n2s = new ArrayList();
	}

	public int GetId(string str)
	{
		if (!m_s2n.Contains(str))
		{
			return -1;
		}
		return (int)m_s2n[str];
	}

	public int Add(string str)
	{
		if (!m_s2n.Contains(str))
		{
			int num = m_n2s.Add(str);
			m_s2n[str] = num;
		}
		return (int)m_s2n[str];
	}

	public int AddReport(string str)
	{
		int count = m_s2n.Count;
		int result = Add(str);
		if (count != m_s2n.Count)
		{
		}
		return result;
	}
}
