using System;
using System.Collections;

namespace CriMw.CriGears.GaPack;

public class FMLog : FMLogReader
{
	public struct ST_L
	{
		public string m_cpkname;

		public string m_groupname;

		public string m_filename;

		public long m_offset;

		public long m_size;

		public ST_L(string cpkname, string groupname, string filename, long adr, long size)
		{
			m_cpkname = cpkname;
			m_groupname = groupname;
			m_filename = filename;
			m_offset = adr;
			m_size = size;
		}

		public void Disp()
		{
			Console.Write("行データ {0:s} {1:s} {2:s} {3:d} {4:d}\n", m_cpkname, m_groupname, m_filename, m_offset, m_size);
		}
	}

	public ArrayList m_log;

	private void L(string cpkname, string groupname, string filename, string adrstr, string sizestr)
	{
		long result;
		bool flag = long.TryParse(adrstr, out result);
		long result2;
		bool flag2 = long.TryParse(sizestr, out result2);
		if (flag && flag2)
		{
			m_log.Add(new ST_L(cpkname, groupname, filename, result, result2));
		}
	}

	public override void Load(string crifs, string timeptr, string pristr, string cmdstr, string filename, string adrstr, string sizestr, string cpkname, string offsetstr, string did)
	{
		L(cpkname, "NOGROUP", filename, adrstr, sizestr);
	}

	public void Disp()
	{
		foreach (ST_L item in m_log)
		{
			item.Disp();
		}
	}

	public FMLog(FMLog oyalog, string cpkname, string groupname)
	{
		m_log = new ArrayList();
		foreach (ST_L item in oyalog.m_log)
		{
			if ((cpkname.Equals("") || item.m_cpkname.Equals(cpkname)) && (groupname.Equals("") || item.m_groupname.Equals(groupname)))
			{
				m_log.Add(item);
			}
		}
	}

	public FMLog()
	{
		m_log = new ArrayList();
	}

	public FMInfos FMLog2FMInfos(FileidMaster fim)
	{
		long[] array = new long[fim.GetCount()];
		for (int i = 0; i < fim.GetCount(); i++)
		{
			array[i] = 0L;
		}
		for (int i = 0; i < m_log.Count; i++)
		{
			ST_L sT_L = (ST_L)m_log[i];
			int id = fim.GetId(sT_L.m_filename);
			long num = sT_L.m_offset + sT_L.m_size;
			if (num > array[id])
			{
				array[id] = num;
			}
		}
		FMInfos fMInfos = new FMInfos(fim.GetCount());
		for (int i = 0; i < fim.GetCount(); i++)
		{
			fMInfos.m_fminfos[i] = new FMInfos.FMInfo(fim.GetFilename(i), array[i]);
		}
		return fMInfos;
	}

	public Infos FMLog2Infos(FileidMaster fim)
	{
		long[] array = new long[fim.GetCount()];
		for (int i = 0; i < fim.GetCount(); i++)
		{
			array[i] = 0L;
		}
		for (int i = 0; i < m_log.Count; i++)
		{
			ST_L sT_L = (ST_L)m_log[i];
			int id = fim.GetId(sT_L.m_filename);
			long num = sT_L.m_offset + sT_L.m_size;
			if (num > array[id])
			{
				array[id] = num;
			}
		}
		Infos infos = new Infos();
		infos.m_fileidmax = fim.GetCount();
		infos.m_infos = new Infos.Info[infos.m_fileidmax];
		for (int i = 0; i < fim.GetCount(); i++)
		{
			infos.m_infos[i] = new Infos.Info(i, array[i]);
		}
		return infos;
	}

	public Logs FMLog2Logs(FileidMaster fim)
	{
		int num = 0;
		for (int i = 0; i < m_log.Count; i++)
		{
			int id = fim.GetId(((ST_L)m_log[i]).m_filename);
			if (id >= 0)
			{
				num++;
			}
		}
		Logs logs = new Logs();
		logs.m_logs = new Logs.Log[num];
		int num2 = 0;
		for (int i = 0; i < m_log.Count; i++)
		{
			ST_L sT_L = (ST_L)m_log[i];
			int id = fim.GetId(sT_L.m_filename);
			if (id >= 0)
			{
				logs.m_logs[num2] = new Logs.Log(id, sT_L.m_offset, sT_L.m_size);
				num2++;
			}
		}
		return logs;
	}

	public long CalcFMInfos(FMInfos fminfos)
	{
		FileidMaster fileidMaster = new FileidMaster();
		fileidMaster.AddAll(fminfos);
		Infos infos = fminfos.FMInfos2Infos(fileidMaster);
		Fseq fseq = fminfos.FMInfos2Fseq(fileidMaster);
		Logs logs = FMLog2Logs(fileidMaster);
		CalcSeekamount calcSeekamount = new CalcSeekamount(infos, fseq);
		return calcSeekamount.Calc(logs);
	}
}
