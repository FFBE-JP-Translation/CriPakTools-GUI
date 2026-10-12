namespace CriMw.CriGears.GaPack;

internal interface IFMLogReader
{
	void Read(string filename);

	void Load(string crifs, string timeptr, string pristr, string cmdstr, string filename, string adrstr, string sizestr, string cpkname, string offsetstr, string did);
}
