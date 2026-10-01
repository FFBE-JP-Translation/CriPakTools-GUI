namespace CriMw.CriGears.GaPack;

public interface ILogs2Fseq
{
	void Exec(NewRecordEvalCardFseq nr);

	EvalCardFseq GetBestFseq();
}
