using System.Collections.Generic;
using CriCpkMaker;

namespace cpkmakecCs;

public class CommandInfo
{
	public enum EnumDataMode
	{
		ModeId,
		ModeFilename,
		ModeFilenameAndId,
		ModeFilenameAndGroup,
		ModeIdAndGroup,
		ModeFilenameIdGroup
	}

	public enum EnumDispMode
	{
		All,
		NoProgress,
		Off
	}

	public enum EnumTypePlatform
	{
		Standard,
		GP1,
		GP2,
		FRV
	}

	public string CsvFilename = null;

	public string CpkFilename = null;

	public bool SetDirectory = false;

	public string HeaderFile = "";

	public int Alignment = 2048;

	public EnumDataMode Mode = EnumDataMode.ModeId;

	public string CpkRootPath = "";

	public bool Display = false;

	public bool Mask = false;

	public bool EternalLoop = false;

	public bool Debug = false;

	public bool ForceCompress = false;

	public bool UseUnfixedMemory = false;

	public bool PressKey = true;

	public bool SafeMode = false;

	public bool EnableMself = false;

	public bool EnableDateTimeInfo = true;

	public bool EnableGroupAttribute = false;

	public bool NoGroupSort = false;

	public bool Duplicate = false;

	public bool CRC = false;

	public bool RandomPadding = false;

	public bool EnableTopTocInfo = true;

	public string LogFilename = null;

	public int LogSortCalcs = 1000;

	public string Comment = null;

	public CAnalyCsvFile.TextCode TextCode = CAnalyCsvFile.TextCode.CODE_SJIS;

	public bool Test = false;

	public string TestParameter = null;

	public CExternalBuffer ExternalBuffer = null;

	public bool DisableLocalInfo = false;

	public bool EnableAdditionalWriting = false;

	public string OriginalCurrentDir = null;

	public bool SwapMask = false;

	public bool DefineHeaderShort = false;

	public bool DefineHeaderLocalFilePath = false;

	public bool DefineHeaderCsvDefOrder = false;

	public string DefineHeaderUniqString = null;

	public int DividedSizeKByte = 0;

	public bool ForceFileUnification = false;

	public float CompPer = 100f;

	public int CompFileSize = 0;

	public EnumDispMode DispMode = EnumDispMode.All;

	public string TargetGroup = null;

	public string AddRangeCsvFname = null;

	public string ExportCsvFname = null;

	public bool EnableAfs2 = false;

	public string ExcludedFilter = null;

	public string UncompFilter = null;

	public EnumTypePlatform platform = EnumTypePlatform.Standard;

	public bool Verify = false;

	public bool BuildAndVerify = false;

	public int GroupDataAlignment = -1;

	public string AttrListFilename = "";

	public string PartOfCpkHeaderExt = "";

	public bool EnableGinf = false;

	public bool EnableHtocFpathToToc = false;

	public bool EnableHtocFidToToc = false;

	public bool EnableHgtocGnameToGlink = false;

	public bool EnableHgtocGanameToGinf = false;

	public bool EnableHgtocGfpathToGfinf = false;

	public bool EnableHgtocGfidToGfinf = false;

	public string CpkprojFilename = null;

	public bool EnableAttributeUserString = false;

	public bool LayoutOrderForCompAndUni = false;

	public int CompFileAlign = 0;

	public EnumCompressCodec CompressCodec = EnumCompressCodec.CodecLayla;

	public int CompressCodecOptionLzmaWindowSize = 3;

	public int CompressCodecOptionLzmaBlockSize = 17;

	public int CompressCodecOptionRelcPageSize = 12;

	public int CompressCodecOptionRelcRomType = 1;

	public Dictionary<string, int> AttrListAlignment = null;

	public bool DummyFileName = false;

	/// <summary>FFBE JP format (--ffbejp): files are encrypted with a key derived from their file name.</summary>
	public bool FfbeJp = false;
}
