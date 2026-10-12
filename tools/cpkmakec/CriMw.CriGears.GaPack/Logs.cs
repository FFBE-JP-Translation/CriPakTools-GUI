using System;

namespace CriMw.CriGears.GaPack;

public class Logs
{
	public class Log
	{
		public int m_id;

		public long m_offset;

		public long m_size;

		public Log(int id)
		{
			m_id = id;
			m_offset = 0L;
			m_size = 0L;
		}

		public Log(int id, long offset, long size)
		{
			m_id = id;
			m_offset = offset;
			m_size = size;
		}

		public void Disp()
		{
			Console.Write("{0:d}:{1:d}:{2:d}\n", m_id, m_offset, m_size);
		}
	}

	public Log[] m_logs;

	public int GetFileId(int ind)
	{
		return m_logs[ind].m_id;
	}

	public int GetSize()
	{
		return m_logs.Length;
	}

	public void Disp()
	{
		Log[] logs = m_logs;
		foreach (Log log in logs)
		{
			log.Disp();
		}
		Console.Write("\n");
	}
}
