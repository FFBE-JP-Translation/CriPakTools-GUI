using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CpkFileBuilder;
using CriMw.CriGears.GaPack;

namespace cpkmakecCs;

internal class GASort
{
	private Hashtable hash = new Hashtable();

	private bool exitFlag = false;

	public bool Sort(CommandInfo cmdinf, List<CMkFileInfo> finfs)
	{
		long min = 0L;
		long max = 0L;
		uint num = 0u;
		using (GAWrap gAWrap = new GAWrap())
		{
			inputFileInfo(cmdinf, finfs, gAWrap);
			gAWrap.Start(cmdinf.LogFilename);
			Console.CancelKeyPress += Console_CancelKeyPress;
			Console.WriteLine("During the calculation of the file layout ... (Stop : CTRL+C)");
			for (int i = 0; i < cmdinf.LogSortCalcs; i++)
			{
				gAWrap.Execute(100, out min, out max);
				int num2 = cmdinf.LogSortCalcs - i - 1;
				if (min == 0)
				{
					Console.Write("Optimazation {2,6}% Calcs. {3}  \r", 0, 0, 100.0, num2.ToString());
				}
				else
				{
					double num3 = (double)min / (double)max * 100.0;
					Console.Write("Optimazation {2,6}% Calcs. {3}  \r", min.ToString("N0"), max.ToString("N0"), num3.ToString("F2"), num2.ToString());
				}
				if (exitFlag)
				{
					break;
				}
			}
			Console.WriteLine(exitFlag ? "\r\nstop." : "\r\ndone.");
			Console.CancelKeyPress -= Console_CancelKeyPress;
			gAWrap.Stop(out min, out max);
			FMInfos result = gAWrap.GetResult();
			FMInfos.FMInfo[] fminfos = result.m_fminfos;
			foreach (FMInfos.FMInfo fMInfo in fminfos)
			{
				CMkFileInfo cMkFileInfo = (CMkFileInfo)hash[fMInfo.m_filename];
				cMkFileInfo.SortIndex = num;
				num++;
			}
		}
		finfs.Sort(new FileInfoSortIndexSorter());
		ExportCsvFile(finfs);
		return true;
	}

	private void ExportCsvFile(List<CMkFileInfo> finfs)
	{
		using StreamWriter streamWriter = new StreamWriter(new FileStream("cpkmaker.out.csv", FileMode.Create), Encoding.UTF8);
		foreach (CMkFileInfo finf in finfs)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(finf.LocalFilePath);
			stringBuilder.Append(", ");
			stringBuilder.Append(finf.ContentFilePath);
			stringBuilder.Append(", ");
			stringBuilder.Append(finf.FileId.ToString());
			stringBuilder.Append(", ");
			stringBuilder.Append(finf.Compress ? "Compress  " : "Uncompress");
			if (!string.IsNullOrEmpty(finf.Groups))
			{
				stringBuilder.Append(", ");
				stringBuilder.Append("\"");
				stringBuilder.Append(finf.Groups);
				stringBuilder.Append("\", ");
				stringBuilder.Append(finf.Attribute);
			}
			streamWriter.WriteLine(stringBuilder.ToString());
		}
	}

	private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
	{
		e.Cancel = true;
		exitFlag = true;
	}

	private void inputFileInfo(CommandInfo cmdinf, List<CMkFileInfo> finfs, GAWrap ga)
	{
		int num = 0;
		ga.InitializeFileCount(finfs.Count);
		foreach (CMkFileInfo finf in finfs)
		{
			num++;
			finf.CompressedSize = finf.OriginalSize;
			finf.ContentFilePath = finf.ContentFilePath.Replace('\\', '/');
			hash.Add(finf.ContentFilePath, finf);
			ga.RegisterFileInfo(finf.ContentFilePath, finf.CompressedSize);
		}
	}
}
