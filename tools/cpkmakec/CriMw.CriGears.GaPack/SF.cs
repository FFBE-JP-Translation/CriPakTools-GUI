using System.IO;

namespace CriMw.CriGears.GaPack;

internal struct SF
{
	public enum Part
	{
		Mae,
		Ato,
		All
	}

	private int m_size;

	public string m_filename;

	public static void KariWrite_Load(TextWriter tw, string groupname, string filename, int start, int size)
	{
		tw.Write("#CRIFS,time,priority,Load, {0:s},{1:d},{2:d},realfilename,realoffset,did\n", filename, start, size);
	}

	public SF(string filename, int size)
	{
		m_size = size;
		m_filename = filename;
	}

	public void Disp(TextWriter tw)
	{
		tw.Write("{0:s} {1:d}\n", m_filename, m_size);
	}

	public void Karilog(TextWriter tw, string groupname, Part part)
	{
		int num;
		int num2;
		switch (part)
		{
		case Part.Mae:
			num = 0;
			num2 = m_size / 2;
			break;
		case Part.Ato:
			num = m_size / 2;
			num2 = m_size;
			break;
		default:
			num = 0;
			num2 = m_size;
			break;
		}
		KariWrite_Load(tw, groupname, m_filename, num, num2 - num);
	}
}
