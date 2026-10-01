using System;

namespace CriMw.CriGears.GaPack;

public class Infos
{
	public class Info
	{
		private int m_id;

		private long m_filesize;

		public Info(int id, long size)
		{
			m_id = id;
			m_filesize = size;
		}

		public void Disp()
		{
			Console.Write("{0:d} : {1:d}\n", m_id, m_filesize);
		}

		public long GetFileSize()
		{
			return m_filesize;
		}
	}

	public Info[] m_infos;

	public int m_fileidmax;

	public long GetFileSize(int id)
	{
		return m_infos[id].GetFileSize();
	}

	public void Disp()
	{
		Console.Write("m_fileidmax={0:d}\n", m_fileidmax);
		Info[] infos = m_infos;
		foreach (Info info in infos)
		{
			info.Disp();
		}
	}
}
