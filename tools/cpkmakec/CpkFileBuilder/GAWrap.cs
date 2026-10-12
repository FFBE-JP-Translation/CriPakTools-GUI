using System;
using CriMw.CriGears.GaPack;

namespace CpkFileBuilder;

public class GAWrap : IDisposable
{
	private class MyNewRecord : NewRecordEvalCardFseq
	{
		public long minrec = long.MaxValue;

		public long maxrec = 0L;

		void NewRecordEvalCardFseq.NewRecord(EvalCardFseq ecf)
		{
			if (maxrec < ecf.m_point64)
			{
				maxrec = ecf.m_point64;
			}
			if (minrec > ecf.m_point64)
			{
				minrec = ecf.m_point64;
			}
		}
	}

	private FMInfos fmInfos;

	private FMLog fmLog;

	private FileidMaster fileIdConv;

	private Logs fmLogConv;

	private Infos infos;

	private ILogs2Fseq fmGaFarm;

	private MyNewRecord fmRecord;

	private int fileCount = -1;

	private int curFileCount;

	private long totalCalcs;

	public long TotalCalcs => totalCalcs;

	public GAWrap()
	{
		fmLog = new FMLog();
		fileIdConv = new FileidMaster();
	}

	public void Dispose()
	{
		fmInfos = null;
		fmLog = null;
		fileIdConv = null;
		fmLogConv = null;
		infos = null;
		if (fmGaFarm != null)
		{
			fmGaFarm = null;
		}
		if (fmRecord != null)
		{
			fmRecord = null;
		}
		GC.Collect();
		GC.WaitForFullGCComplete();
		GC.Collect();
	}

	public void InitializeFileCount(int count)
	{
		fileCount = count;
		if (fmInfos != null)
		{
			throw new Exception("既に初期化済み");
		}
		fmInfos = new FMInfos(fileCount);
		curFileCount = 0;
	}

	public void RegisterFileInfo(string filePath, long fileSize)
	{
		fmInfos.m_fminfos[curFileCount] = new FMInfos.FMInfo(filePath, fileSize);
		curFileCount++;
	}

	public bool Start(string csvFilePath, out long min, out long max)
	{
		totalCalcs = 0L;
		if (curFileCount != fileCount)
		{
			throw new Exception("ファイル情報が全て入力されていません");
		}
		if (fmRecord != null)
		{
			throw new Exception("再利用不可");
		}
		GC.Collect();
		GC.WaitForFullGCComplete();
		GC.Collect();
		fmLog.Read(csvFilePath);
		fileIdConv.AddAll(fmInfos);
		fmLogConv = fmLog.FMLog2Logs(fileIdConv);
		infos = fmInfos.FMInfos2Infos(fileIdConv);
		fmGaFarm = new CLogs2FseqGaFarm(fmLogConv, infos);
		fmRecord = new MyNewRecord();
		UpdateRecord(out min, out max);
		return true;
	}

	public bool Start(string csvFilePath)
	{
		long min;
		long max;
		return Start(csvFilePath, out min, out max);
	}

	private void UpdateRecord(out long min, out long max)
	{
		min = fmRecord.minrec;
		max = fmRecord.maxrec;
	}

	public string Execute(int numLoop)
	{
		Execute(numLoop, out var min, out var max);
		return string.Format("min {0} / max {1}", min.ToString("N0"), max.ToString("N0"));
	}

	public void Execute(int numLoop, out long min, out long max)
	{
		for (int i = 0; i < numLoop; i++)
		{
			fmGaFarm.Exec(fmRecord);
		}
		totalCalcs += numLoop;
		UpdateRecord(out min, out max);
	}

	public void Stop(out long min, out long max)
	{
		EvalCardFseq bestFseq = fmGaFarm.GetBestFseq();
		FMInfos fMInfos = SeqFim2FMInfos(bestFseq, infos, fileIdConv);
		UpdateRecord(out min, out max);
		GC.Collect();
		GC.WaitForFullGCComplete();
		GC.Collect();
	}

	public string Stop()
	{
		Stop(out var min, out var max);
		return string.Format("min {0} / max {1}", min.ToString("N0"), max.ToString("N0"));
	}

	public FMInfos GetResult()
	{
		EvalCardFseq bestFseq = fmGaFarm.GetBestFseq();
		return SeqFim2FMInfos(bestFseq, infos, fileIdConv);
	}

	private static FMInfos SeqFim2FMInfos(EvalCardFseq seq, Infos infos, FileidMaster fim)
	{
		FMInfos fMInfos = new FMInfos(seq.m_fseq.GetSize());
		for (int i = 0; i < seq.m_fseq.GetSize(); i++)
		{
			int fileid = seq.m_fseq.GetFileid(i);
			fMInfos.m_fminfos[i] = new FMInfos.FMInfo(fim.GetFilename(fileid), infos.GetFileSize(fileid));
		}
		return fMInfos;
	}
}
