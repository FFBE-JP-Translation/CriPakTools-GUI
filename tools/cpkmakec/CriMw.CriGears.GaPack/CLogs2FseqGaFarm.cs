using System;

namespace CriMw.CriGears.GaPack;

public class CLogs2FseqGaFarm : LogsInfosGaFarm, ILogs2Fseq
{
	private static Random MakeRandom(int randseed)
	{
		if (randseed == -1)
		{
			return new Random();
		}
		return new Random(randseed);
	}

	public CLogs2FseqGaFarm(Random farmrandom, Logs logs, Infos infos)
		: base(new GaBirthFromLogs(logs, farmrandom), logs, infos, farmrandom)
	{
	}

	public CLogs2FseqGaFarm(Logs logs, Infos infos, int randseed)
		: this(MakeRandom(randseed), logs, infos)
	{
	}

	public CLogs2FseqGaFarm(Logs logs, Infos infos)
		: this(logs, infos, -1)
	{
	}

	public CLogs2FseqGaFarm(Random farmrandom, Logs logs, Infos infos, EvalCardFseq lastfseq)
		: base(new GaBirthFromSeq(lastfseq.m_fseq, farmrandom), logs, infos, farmrandom)
	{
	}

	public CLogs2FseqGaFarm(Logs logs, Infos infos, EvalCardFseq lastfseq, int randseed)
		: this(MakeRandom(randseed), logs, infos, lastfseq)
	{
	}

	public CLogs2FseqGaFarm(Logs logs, Infos infos, EvalCardFseq lastfseq)
		: this(logs, infos, lastfseq, -1)
	{
	}

	void ILogs2Fseq.Exec(NewRecordEvalCardFseq nr)
	{
		Exec(nr);
	}

	EvalCardFseq ILogs2Fseq.GetBestFseq()
	{
		return m_minimum;
	}
}
