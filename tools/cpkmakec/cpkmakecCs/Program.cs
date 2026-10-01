using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using CriCpkMaker;

namespace cpkmakecCs;

public class Program
{
	private const int EXIT_SUCCESS = 0;

	private const int EXIT_FAILURE = 1;

	private const int SIZE_UNCOMPRESS = 128;

	private const string StringOutCpkH = ".cpkh";

	private const string StringCompleted = "completed.";

	private static void ErrorExit(CommandInfo cmdinf, string str)
	{
		Console.WriteLine("\r\nError : " + str);
		if (cmdinf.PressKey)
		{
			Console.WriteLine("error stop. <press any key>");
			Console.ReadKey();
		}
		Environment.Exit(1);
	}

	private static string MakeProgressString(double prg)
	{
		int num = 0;
		num = ((prg != 0.0) ? ((!(prg >= 100.0)) ? ((int)(prg / 3.0)) : 33) : 0);
		string text = "|";
		for (int i = 0; i < num; i++)
		{
			text += "#";
		}
		for (int i = 0; i < 33 - num; i++)
		{
			text += "-";
		}
		return text + "|";
	}

	private static bool ShowCpkInfo(CommandInfo cmdinf)
	{
		string cpkFilename = cmdinf.CpkFilename;
		using (CpkMaker cpkMaker = new CpkMaker())
		{
			if (!cpkMaker.AnalyzeCpkFile(cpkFilename))
			{
				return false;
			}
			if (cpkMaker.EnableAfs2)
			{
				return true;
			}
			Console.WriteLine("===================== View Mode =====================");
			Console.Write("detecting...\r");
			string cpkInformationString = cpkMaker.GetCpkInformationString();
			if (!string.IsNullOrEmpty(cmdinf.ExportCsvFname))
			{
				SaveCsvFileCpkInfo(cpkMaker, cmdinf);
			}
			Console.WriteLine(cpkInformationString + "done.");
		}
		return true;
	}

	private static bool SaveCsvFileCpkInfo(CpkMaker maker, CommandInfo cmdinf)
	{
		try
		{
			using (StreamWriter streamWriter = new StreamWriter(cmdinf.ExportCsvFname, append: false))
			{
				foreach (CFileInfo fileInfo in maker.FileData.FileInfos)
				{
					StringBuilder stringBuilder = new StringBuilder();
					stringBuilder.Append(fileInfo.ContentFilePath);
					stringBuilder.Append(",");
					stringBuilder.Append(fileInfo.FileId.ToString());
					stringBuilder.Append(",");
					stringBuilder.Append(fileInfo.Extractsize.ToString());
					stringBuilder.Append(",");
					stringBuilder.Append(fileInfo.Filesize.ToString());
					stringBuilder.Append(",");
					stringBuilder.Append(fileInfo.CompressPercentage.ToString("F2"));
					stringBuilder.Append(",");
					stringBuilder.Append(fileInfo.AttributeString);
					stringBuilder.Append(",\"");
					stringBuilder.Append(fileInfo.GroupString);
					stringBuilder.Append("\",");
					stringBuilder.Append(fileInfo.DateTimeString);
					stringBuilder.Append(",");
					stringBuilder.Append(fileInfo.Offset.ToString());
					stringBuilder.Append(",");
					stringBuilder.Append(fileInfo.LocalFilePath);
					streamWriter.WriteLine(stringBuilder.ToString());
				}
				streamWriter.Close();
			}
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine("Failed to save a CSV File. " + ex.Message);
			return false;
		}
	}

	private static bool SwapCpkFileMask(string fname)
	{
		CpkMaker cpkMaker = new CpkMaker();
		if (!cpkMaker.AnalyzeCpkFile(fname))
		{
			cpkMaker.Dispose();
			return false;
		}
		if (cpkMaker.SwapCpkMask(fname))
		{
			Console.WriteLine("Mask : " + cpkMaker.Mask);
			Console.WriteLine("done.");
		}
		return true;
	}

	private static void PreProcess(CommandInfo cmdinf)
	{
		if (!cmdinf.UseUnfixedMemory)
		{
			Console.WriteLine();
			cmdinf.ExternalBuffer = new CExternalBuffer();
			if (!cmdinf.ExternalBuffer.SetBuffers(134217728, 268435456, 134217728))
			{
				Console.WriteLine("Could not allocate memory enough for working.");
				cmdinf.ExternalBuffer = null;
			}
			Console.WriteLine("");
		}
	}

	private static string NormalizeString(string instr)
	{
		if (string.IsNullOrEmpty(instr))
		{
			return "";
		}
		return instr.Replace('\\', '/').Trim(new char[1] { '/' });
	}

	private static bool IsMatchFileInfo(CMkFileInfo file, CFileInfo finf, bool enableGroup)
	{
		bool flag = file.Compress == finf.IsCompressed;
		if (!flag && file.OriginalSize <= 128)
		{
			flag = true;
		}
		if (file.CompressAdd)
		{
			flag = true;
		}
		if (file.OriginalSize == (long)finf.Extractsize && flag && file.IsSameTime(finf.DateTime))
		{
			if (!enableGroup)
			{
				return true;
			}
			string text = NormalizeString(file.Attribute);
			string text2 = NormalizeString(finf.AttributeString);
			if (text == text2)
			{
				string text3 = NormalizeString(file.Groups);
				string text4 = NormalizeString(finf.GroupString);
				if (text4 == "(none)")
				{
					text4 = "";
				}
				if (text3 == text4)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static int MakeByCsvFile(CommandInfo cmdinf)
	{
		int result = 0;
		using (CpkMaker maker = MakeByCsvFileSubInitialize(cmdinf))
		{
			List<CMkFileInfo> list = MakeByCsvFileSubAanlyCsvAndSortFinf(cmdinf, maker);
			if (list == null)
			{
				return 1;
			}
			result = (cmdinf.Verify ? Verify(cmdinf, list) : MakeByCsvFileSubBuild(cmdinf, maker, list));
		}
		FfbeJp.Cleanup();
		return result;
	}

	private static CpkMaker MakeByCsvFileSubInitialize(CommandInfo cmdinf)
	{
		Printf(cmdinf, "detecting ... \r");
		bool usedlg = true;
		if (cmdinf.SafeMode)
		{
			usedlg = false;
		}
		CpkMaker cpkMaker = new CpkMaker(usedlg);
		cpkMaker.BaseDirectory = "";
		cpkMaker.CpkRootDirectory = cmdinf.CpkRootPath;
		cpkMaker.DataAlign = (uint)cmdinf.Alignment;
		cpkMaker.Mask = cmdinf.Mask;
		cpkMaker.ForceCompress = cmdinf.ForceCompress;
		cpkMaker.Comment = cmdinf.Comment;
		cpkMaker.ToolVersion = "CPKMC" + CFileUtil.GetApplicationVersionFromResource();
		cpkMaker.ExternalBuffer = cmdinf.ExternalBuffer;
		cpkMaker.EnableDateTimeInfo = cmdinf.EnableDateTimeInfo;
		cpkMaker.EnableMself = cmdinf.EnableMself;
		cpkMaker.EnableDuplicateByGroup = cmdinf.Duplicate;
		cpkMaker.SortContinuousGroupFile = !cmdinf.NoGroupSort;
		cpkMaker.EnableCrc = cmdinf.CRC;
		cpkMaker.RandomPadding = cmdinf.RandomPadding;
		cpkMaker.EnableTopTocInfo = cmdinf.EnableTopTocInfo;
		cpkMaker.DefineHeaderLocalFilePath = cmdinf.DefineHeaderLocalFilePath;
		cpkMaker.DefineHeaderUniqString = cmdinf.DefineHeaderUniqString;
		cpkMaker.CompressCodec = ((cmdinf.DividedSizeKByte > 0) ? EnumCompressCodec.CodecDpk : cmdinf.CompressCodec);
		cpkMaker.DpkDividedSize = cmdinf.DividedSizeKByte * 1024;
		cpkMaker.DefineHeaderCsvOrder = cmdinf.DefineHeaderCsvDefOrder;
		cpkMaker.CompPercentage = cmdinf.CompPer;
		cpkMaker.CompFileSize = cmdinf.CompFileSize;
		cpkMaker.CompFileAlign = cmdinf.CompFileAlign;
		cpkMaker.EnableGInfo = cmdinf.EnableGinf;
		cpkMaker.EnableHtocFpathToToc = cmdinf.EnableHtocFpathToToc;
		cpkMaker.EnableHtocFidToToc = cmdinf.EnableHtocFidToToc;
		cpkMaker.EnableHgtocGnameToGlink = cmdinf.EnableHgtocGnameToGlink;
		cpkMaker.EnableHgtocGanameToGinf = cmdinf.EnableHgtocGanameToGinf;
		cpkMaker.EnableHgtocGfpathToGfinf = cmdinf.EnableHgtocGfpathToGfinf;
		cpkMaker.EnableHgtocGfidToGfinf = cmdinf.EnableHgtocGfidToGfinf;
		cpkMaker.LayoutOrderForCompAndUni = cmdinf.LayoutOrderForCompAndUni;
		cpkMaker.PartOfCpkHeaderExt = cmdinf.PartOfCpkHeaderExt;
		if (cmdinf.UncompFilter != null)
		{
			cpkMaker.UncompFileExt = cmdinf.UncompFilter;
		}
		if (cmdinf.GroupDataAlignment > 0)
		{
			cpkMaker.GroupDataAlignment = cmdinf.GroupDataAlignment;
		}
		cpkMaker.EnableAttributeUserString = cmdinf.EnableAttributeUserString;
		CodecOptions codecOptions = new CodecOptions();
		codecOptions.m_window_size = (uint)cmdinf.CompressCodecOptionLzmaWindowSize;
		codecOptions.m_block_size = (uint)cmdinf.CompressCodecOptionLzmaBlockSize;
		codecOptions.m_page_size = (uint)cmdinf.CompressCodecOptionRelcPageSize;
		codecOptions.m_rom_type = (uint)cmdinf.CompressCodecOptionRelcRomType;
		cpkMaker.codecOptions = codecOptions;
		bool flag = false;
		switch (cmdinf.Mode)
		{
		case CommandInfo.EnumDataMode.ModeId:
			cpkMaker.CpkFileMode = CpkMaker.EnumCpkFileMode.ModeId;
			break;
		case CommandInfo.EnumDataMode.ModeFilename:
			cpkMaker.CpkFileMode = CpkMaker.EnumCpkFileMode.ModeFilename;
			break;
		case CommandInfo.EnumDataMode.ModeFilenameAndId:
			cpkMaker.CpkFileMode = CpkMaker.EnumCpkFileMode.ModeFilenameAndId;
			cpkMaker.EnableFileName = !cmdinf.DummyFileName;
			break;
		case CommandInfo.EnumDataMode.ModeFilenameAndGroup:
			cpkMaker.CpkFileMode = CpkMaker.EnumCpkFileMode.ModeFilenameAndGroup;
			flag = true;
			break;
		case CommandInfo.EnumDataMode.ModeIdAndGroup:
			cpkMaker.CpkFileMode = CpkMaker.EnumCpkFileMode.ModeIdAndGroup;
			flag = true;
			break;
		case CommandInfo.EnumDataMode.ModeFilenameIdGroup:
			cpkMaker.CpkFileMode = CpkMaker.EnumCpkFileMode.ModeFilenameIdGroup;
			flag = true;
			break;
		default:
			throw new Exception("Unknown mode");
		}
		if (cmdinf.ForceFileUnification)
		{
			if (cpkMaker.CpkFileMode == CpkMaker.EnumCpkFileMode.ModeId)
			{
				Console.WriteLine("Warning: Cannot use \"ID only\" and File Unification.");
			}
			else
			{
				cpkMaker.UnificationMode = EnumUnificationMode.ModeFileAll;
			}
		}
		cpkMaker.FileLayoutMode = CpkMaker.EnumFileLayoutMode.LayoutModeUser;
		if (flag && cmdinf.Duplicate)
		{
			cpkMaker.FileLayoutMode = CpkMaker.EnumFileLayoutMode.LayoutModeGroup;
		}
		if (cmdinf.DisableLocalInfo)
		{
			cpkMaker.EnableEtoc = false;
		}
		return cpkMaker;
	}

	private static void CreateCsvFile(CommandInfo cmdinf)
	{
		string csvFilename = cmdinf.CsvFilename;
		string[] directories = Directory.GetDirectories(csvFilename, "*.*", SearchOption.AllDirectories);
		string[] array = Directory.GetFiles(csvFilename, "*.*", SearchOption.AllDirectories);
		if (array == null || array.Length <= 0)
		{
			return;
		}
		if (!string.IsNullOrEmpty(cmdinf.ExcludedFilter))
		{
			List<string> list = new List<string>();
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (!CFileUtil.IsMatchByWildcard(text, cmdinf.ExcludedFilter))
				{
					list.Add(text);
				}
			}
			if (list.Count == 0)
			{
				return;
			}
			array = list.ToArray();
		}
		using (StreamWriter streamWriter = new StreamWriter(new FileStream("cpkmaker.out.csv", FileMode.Create), Encoding.UTF8))
		{
			uint num = 0u;
			string[] array2 = array;
			foreach (string text in array2)
			{
				string text2 = text.Substring(csvFilename.Length);
				streamWriter.WriteLine(text + ", " + text2 + ", " + num.ToString("D") + ", Uncompress");
				num++;
			}
		}
		cmdinf.CsvFilename = "cpkmaker.out.csv";
		cmdinf.TextCode = CAnalyCsvFile.TextCode.CODE_UTF8;
	}

	private static bool IsEnableGroup(CommandInfo.EnumDataMode mode)
	{
		if (mode == CommandInfo.EnumDataMode.ModeFilenameAndGroup || mode == CommandInfo.EnumDataMode.ModeIdAndGroup || mode == CommandInfo.EnumDataMode.ModeFilenameIdGroup)
		{
			return true;
		}
		return false;
	}

	private static List<CMkFileInfo> MakeByCsvFileSubAanlyCsvAndSortFinf(CommandInfo cmdinf, CpkMaker maker)
	{
		List<CMkFileInfo> list = new List<CMkFileInfo>();
		uint num = 0u;
		try
		{
			using (CAnalyCsvFile cAnalyCsvFile = new CAnalyCsvFile())
			{
				if (Directory.Exists(cmdinf.CsvFilename))
				{
					if (cmdinf.EnableGroupAttribute)
					{
						ErrorExit(cmdinf, "Cannot found the group information.");
					}
					CreateCsvFile(cmdinf);
				}
				if (!File.Exists(cmdinf.CsvFilename) && cmdinf.OriginalCurrentDir != null)
				{
					cmdinf.CsvFilename = Path.Combine(cmdinf.OriginalCurrentDir, cmdinf.CsvFilename);
				}
				if (!File.Exists(cmdinf.CsvFilename))
				{
					ErrorExit(cmdinf, CFileUtil.AddDoubleQuote(Path.GetFileName(cmdinf.CsvFilename)) + " is not found.");
				}
				cAnalyCsvFile.AnalizeCsvFile(cmdinf.CsvFilename, cmdinf.TextCode);
				if (cmdinf.TargetGroup != null)
				{
					cAnalyCsvFile.SetTargetGroup(cmdinf.TargetGroup);
				}
				while (true)
				{
					bool flag = true;
					CMkFileInfo cMkFileInfo = new CMkFileInfo();
					if (cAnalyCsvFile.GetLine(cMkFileInfo))
					{
						if (cMkFileInfo.IsFileExist())
						{
							cMkFileInfo.RegisterIndex = num;
							list.Add(cMkFileInfo);
						}
						else
						{
							maker.Dispose();
							ErrorExit(cmdinf, "Line." + cMkFileInfo.Lines + " " + CFileUtil.AddDoubleQuote(Path.GetFileName(cmdinf.CsvFilename)) + " File not found. \"" + cMkFileInfo.LocalFilePath + "\"");
						}
						num++;
						continue;
					}
					break;
				}
			}
			PrintLine(cmdinf, num.ToString("N0") + " file(s) detected. \n");
			if (num == 0)
			{
				maker.Dispose();
				ErrorExit(cmdinf, "\"" + cmdinf.CsvFilename.ToString() + "\" is empty file.");
			}
			if (cmdinf.EnableAdditionalWriting)
			{
				CheckAdditionalFile(cmdinf, list, maker);
			}
			if (cmdinf.LogFilename != null)
			{
				GASort gASort = new GASort();
				gASort.Sort(cmdinf, list);
			}
			else if (!cmdinf.NoGroupSort)
			{
				switch (cmdinf.Mode)
				{
				case CommandInfo.EnumDataMode.ModeFilenameAndGroup:
				case CommandInfo.EnumDataMode.ModeIdAndGroup:
				case CommandInfo.EnumDataMode.ModeFilenameIdGroup:
					list.Sort(new FileInfoFileGroupRegistSorter());
					break;
				}
			}
			if (cmdinf.FfbeJp)
			{
				if (cmdinf.ForceCompress)
				{
					ErrorExit(cmdinf, "--ffbejp cannot be used with -forcecompress (encrypted data does not compress).");
				}
				PrintLine(cmdinf, "FFBE JP format      : encrypting " + list.Count.ToString("N0") + " file(s)");
			}
			foreach (CMkFileInfo item in list)
			{
				if (cmdinf.FfbeJp)
				{
					if (item.Compress)
					{
						ErrorExit(cmdinf, "Line." + item.Lines + " compressed files cannot be used with --ffbejp.");
					}
					item.LocalFilePath = FfbeJp.EncryptToTemp(item.LocalFilePath, string.IsNullOrEmpty(item.ContentFilePath) ? item.LocalFilePath : item.ContentFilePath, item.RegisterIndex);
				}
				int regId = (cmdinf.DefineHeaderCsvDefOrder ? ((int)item.Lines) : (-1));
				if (IsEnableGroup(cmdinf.Mode))
				{
					uint dataAlign = (uint)cmdinf.Alignment;
					if (cmdinf.AttrListAlignment != null && cmdinf.AttrListAlignment.ContainsKey(item.Attribute))
					{
						dataAlign = (uint)cmdinf.AttrListAlignment[item.Attribute];
					}
					maker.AddFile(item.LocalFilePath, item.ContentFilePath, item.FileId, item.Compress, item.Groups, item.Attribute, dataAlign, uniTarget: false, regId);
				}
				else if (!cmdinf.DummyFileName)
				{
					maker.AddFile(item.LocalFilePath, item.ContentFilePath, item.FileId, item.Compress, null, null, 0u, uniTarget: false, regId);
				}
				else
				{
					maker.AddFile(item.LocalFilePath, "unified", item.FileId, item.Compress, null, null, 0u, uniTarget: false, regId);
				}
			}
		}
		catch (Exception ex)
		{
			PrintLine(cmdinf, ex.Message);
			maker.Dispose();
			return null;
		}
		return list;
	}

	private static int MakeByCsvFileSubBuild(CommandInfo cmdinf, CpkMaker maker, List<CMkFileInfo> finfs)
	{
		int num = 0;
		if (!cmdinf.EnableAfs2)
		{
			maker.StartToBuild(cmdinf.CpkFilename);
		}
		else
		{
			maker.StartToBuildAfs2(cmdinf.CpkFilename);
		}
		PrintLine(cmdinf, "Building a CPK file ...");
		string text = "";
		ulong num2 = 0uL;
		while (true)
		{
			bool flag = true;
			Status status = maker.Execute();
			double progress = maker.GetProgress();
			if (text != maker.WorkingMessage)
			{
				if (cmdinf.Debug)
				{
					PrintLine(cmdinf, progress.ToString("F2").PadLeft(7) + "% " + maker.GetElapsedTime().ToString().Substring(0, 8) + " " + maker.WorkingMessage);
				}
				else
				{
					PrintfProgress(cmdinf, "Remain Time " + maker.GetRemainTimeString() + " (" + maker.GetElapsedTime().ToString().Substring(0, 8) + "), " + progress.ToString("F2").PadLeft(7) + "% " + MakeProgressString(maker.GetProgress()) + "\r");
					Console.Title = progress.ToString("F2") + "%";
				}
			}
			if (status == Status.Error)
			{
				ErrorExit(cmdinf, maker.ErrorMessage);
			}
			if (status == Status.Complete)
			{
				break;
			}
			text = maker.WorkingMessage;
			num2++;
		}
		PrintLine(cmdinf, "");
		if (!cmdinf.HeaderFile.Equals(""))
		{
			maker.ExportHeader(cmdinf.HeaderFile, maker.CpkFileMode, cmdinf.DefineHeaderShort);
		}
		if (!string.IsNullOrEmpty(cmdinf.AddRangeCsvFname))
		{
			maker.SaveAddRangeCsvFile(cmdinf.AddRangeCsvFname, cmdinf.CpkFilename);
		}
		if (num == 0)
		{
			PrintLine(cmdinf, "completed.\r\n");
		}
		if (cmdinf.Display)
		{
			ShowCpkInfo(cmdinf);
		}
		if (cmdinf.Test)
		{
			num = ((!testCpk(cmdinf.CpkFilename, cmdinf.TestParameter)) ? 1 : 0);
		}
		return num;
	}

	private static bool AnalyCommandArgs(string[] args, CommandInfo cmdinf)
	{
		CAnalyCmdArgs cAnalyCmdArgs = new CAnalyCmdArgs();
		cAnalyCmdArgs.AnalizeCmdArgs(args);
		if (cAnalyCmdArgs.GetMatchArgs("NOERRORSTOP") != null)
		{
			cmdinf.PressKey = false;
		}
		if (cAnalyCmdArgs.GetMatchArgs("FFBEJP") != null || cAnalyCmdArgs.GetMatchArgs("-FFBEJP") != null)
		{
			cmdinf.FfbeJp = true;
		}
		if (cAnalyCmdArgs.Filename.Count < 1)
		{
			ErrorExit(cmdinf, "less arguments.");
		}
		string text = cAnalyCmdArgs.Filename[0];
		ArgsCmdInfo matchArgs;
		if (CFileUtil.IsMatchFileFourCC(text, "CPK ") || CFileUtil.IsMatchFileFourCC(text, "AFS2"))
		{
			if (File.Exists(text))
			{
				bool flag = true;
				if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("TEST")) != null)
				{
					flag = testCpk(text, matchArgs.ParamString);
				}
				else
				{
					if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("EXPORTCPKINFO")) != null)
					{
						cmdinf.ExportCsvFname = matchArgs.ParamString;
						PrintLine(cmdinf, "Export CSV Filename   : " + cmdinf.ExportCsvFname);
					}
					if (cAnalyCmdArgs.GetMatchArgs("SWAPMASK") != null)
					{
						SwapCpkFileMask(text);
					}
					if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("EXTRACT")) != null)
					{
						string text2 = text.Substring(0, text.Length - Path.GetExtension(text).Length);
						if (!string.IsNullOrEmpty(matchArgs.ParamString))
						{
							text2 = matchArgs.ParamString;
						}
						PrintLine(cmdinf, "Output Directory : " + text2);
						Printf(cmdinf, "Extracting ...  ");
						using CpkMaker cpkMaker = new CpkMaker();
						cpkMaker.AnalyzeCpkFile(text);
						cpkMaker.StartToExtract(text2);
						if (cpkMaker.WaitForComplete())
						{
							if (cmdinf.FfbeJp)
							{
								PrintLine(cmdinf, "Decrypting (FFBE JP) ... " + FfbeJp.DecryptDirectory(text2) + " file(s)");
							}
							PrintLine(cmdinf, "Complete.");
						}
						else
						{
							PrintLine(cmdinf, "Error occured. " + cpkMaker.ErrorMessage);
						}
					}
					else
					{
						cmdinf.CpkFilename = text;
						ShowCpkInfo(cmdinf);
					}
				}
				Environment.Exit((!flag) ? 1 : 0);
			}
			ErrorExit(cmdinf, CFileUtil.AddDoubleQuote(cAnalyCmdArgs.Filename[0]) + " is not found.");
		}
		else if (cAnalyCmdArgs.Filename.Count == 1)
		{
			ErrorExit(cmdinf, CFileUtil.AddDoubleQuote(cAnalyCmdArgs.Filename[0]) + " is invalid file.");
		}
		if (cAnalyCmdArgs.Filename.Count < 2)
		{
			ErrorExit(cmdinf, "less arguments.");
		}
		cmdinf.CsvFilename = cAnalyCmdArgs.Filename[0];
		cmdinf.CpkFilename = CFileUtil.AddExtention(cAnalyCmdArgs.Filename[1], ".cpk");
		if (cmdinf.platform == CommandInfo.EnumTypePlatform.GP2)
		{
			cmdinf.CRC = true;
		}
		if (cAnalyCmdArgs.Command.Count > 0)
		{
			if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("ALIGN")) != null)
			{
				if (matchArgs.ParamValue.HasValue && matchArgs.ParamValue > 0)
				{
					cmdinf.Alignment = matchArgs.ParamValue.Value;
				}
				else
				{
					ErrorExit(cmdinf, "invalidate alignment.");
				}
			}
			if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("CALCS")) != null)
			{
				if (matchArgs.ParamValue.HasValue && matchArgs.ParamValue > 0)
				{
					cmdinf.LogSortCalcs = matchArgs.ParamValue.Value;
				}
				else
				{
					ErrorExit(cmdinf, "invalidate -calc parameter.");
				}
			}
			if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("CODE")) != null)
			{
				if (matchArgs.IsMatchParams("SJIS", "S-JIS"))
				{
					cmdinf.TextCode = CAnalyCsvFile.TextCode.CODE_SJIS;
				}
				else if (matchArgs.IsMatchParams("UTF-8", "UTF8"))
				{
					cmdinf.TextCode = CAnalyCsvFile.TextCode.CODE_UTF8;
				}
				else if (matchArgs.IsMatchParam("EUC"))
				{
					cmdinf.TextCode = CAnalyCsvFile.TextCode.CODE_EUC;
				}
				else
				{
					ErrorExit(cmdinf, "unknown code.");
				}
			}
			if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("MODE")) != null)
			{
				Printf(cmdinf, "CPK Data Mode         : ");
				if (matchArgs.IsMatchParam("ID"))
				{
					cmdinf.Mode = CommandInfo.EnumDataMode.ModeId;
					PrintLine(cmdinf, "ID");
				}
				else if (matchArgs.IsMatchParam("FILENAME"))
				{
					cmdinf.Mode = CommandInfo.EnumDataMode.ModeFilename;
					PrintLine(cmdinf, "Filename");
				}
				else if (matchArgs.IsMatchParam("FULL") || matchArgs.IsMatchParam("FILENAMEID") || matchArgs.IsMatchParam("IDFILENAME"))
				{
					cmdinf.Mode = CommandInfo.EnumDataMode.ModeFilenameAndId;
					PrintLine(cmdinf, "Filename + ID");
				}
				else if (matchArgs.IsMatchParam("GROUP") || matchArgs.IsMatchParam("FILENAMEGROUP") || matchArgs.IsMatchParam("GROUPFILENAME"))
				{
					cmdinf.Mode = CommandInfo.EnumDataMode.ModeFilenameAndGroup;
					cmdinf.EnableGroupAttribute = true;
					PrintLine(cmdinf, "Filename + Group (Attribute)");
				}
				else if (matchArgs.IsMatchParam("IDGROUP") || matchArgs.IsMatchParam("GROUPID"))
				{
					cmdinf.Mode = CommandInfo.EnumDataMode.ModeIdAndGroup;
					cmdinf.EnableGroupAttribute = true;
					PrintLine(cmdinf, "ID + Group (Attribute)");
				}
				else if (matchArgs.IsMatchParam("FILENAMEIDGROUP") || matchArgs.IsMatchParam("IDFILENAMEGROUP"))
				{
					cmdinf.Mode = CommandInfo.EnumDataMode.ModeFilenameIdGroup;
					cmdinf.EnableGroupAttribute = true;
					PrintLine(cmdinf, "Filename + ID + Group (Attribute)");
				}
				else if (matchArgs.IsMatchParam("EXID"))
				{
					cmdinf.Mode = CommandInfo.EnumDataMode.ModeFilenameAndId;
					cmdinf.DisableLocalInfo = true;
					cmdinf.DummyFileName = true;
					PrintLine(cmdinf, "EXID (Extend ID format)");
				}
				else
				{
					ErrorExit(cmdinf, "unknown mode.");
				}
			}
			if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("CPKROOT")) != null)
			{
				cmdinf.CpkRootPath = matchArgs.ParamString;
				PrintLine(cmdinf, "CPK Root Path         : \"" + cmdinf.CpkRootPath + "\"");
			}
			if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("HEADER")) != null)
			{
				if (matchArgs.ParamString == null)
				{
					cmdinf.HeaderFile = CFileUtil.ChangeExtention(cmdinf.CpkFilename, ".h");
				}
				else
				{
					cmdinf.HeaderFile = matchArgs.ParamString;
				}
				PrintLine(cmdinf, "Output Header         : " + cmdinf.HeaderFile);
			}
			if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("HD")) != null)
			{
				cmdinf.DefineHeaderUniqString = ((matchArgs.ParamString == null) ? "" : matchArgs.ParamString);
				PrintLine(cmdinf, "Unique Header Def.    : " + cmdinf.DefineHeaderUniqString);
			}
			if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("OUTCPKH")) != null)
			{
				PrintLine(cmdinf, "Output CPK Header     : Enabled");
				cmdinf.PartOfCpkHeaderExt = ".cpkh";
			}
			if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("EXCLUDED")) != null)
			{
				cmdinf.ExcludedFilter = matchArgs.ParamString;
			}
			if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("COMPPER")) != null || (matchArgs = cAnalyCmdArgs.GetMatchArgs("COMPRESSPERCENTAGE")) != null)
			{
				if (float.TryParse(matchArgs.ParamString, out cmdinf.CompPer))
				{
					PrintLine(cmdinf, "Compress Threshold %  : " + cmdinf.CompPer.ToString("F3") + "%");
				}
				else
				{
					cmdinf.CompPer = 100f;
				}
			}
			if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("COMPFILESIZE")) != null)
			{
				if (int.TryParse(matchArgs.ParamString, out cmdinf.CompFileSize))
				{
					PrintLine(cmdinf, "Comp OriginalFileSize : " + cmdinf.CompFileSize);
				}
				else
				{
					cmdinf.CompFileSize = 0;
				}
			}
			if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("COMPFILEALIGN")) != null && !int.TryParse(matchArgs.ParamString, out cmdinf.CompFileAlign))
			{
				cmdinf.CompFileAlign = 0;
			}
			if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("COMPRESS_CODEC")) != null || (matchArgs = cAnalyCmdArgs.GetMatchArgs("COMPCODEC")) != null)
			{
				if (matchArgs.IsMatchParam("LAYLA"))
				{
					cmdinf.CompressCodec = EnumCompressCodec.CodecLayla;
					PrintLine(cmdinf, "Compress Codec        : Layla");
				}
				else if (matchArgs.IsMatchParam("LZMA"))
				{
					cmdinf.CompressCodec = EnumCompressCodec.CodecLZMA;
					PrintLine(cmdinf, "Compress Codec        : LZMA");
				}
				else if (matchArgs.IsMatchParam("RELC"))
				{
					cmdinf.CompressCodec = EnumCompressCodec.CodecRELC;
					PrintLine(cmdinf, "Compress Codec        : RELC");
				}
				else
				{
					ErrorExit(cmdinf, "Compress Codec        : unknown");
				}
			}
			if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("LZMA_WINDOWSIZE")) != null || (matchArgs = cAnalyCmdArgs.GetMatchArgs("LZMAWINDOWSIZE")) != null)
			{
				if (matchArgs.ParamValue.HasValue)
				{
					int? paramValue = matchArgs.ParamValue;
					if (paramValue.GetValueOrDefault() >= 0 && paramValue.HasValue && matchArgs.ParamValue <= 7)
					{
						cmdinf.CompressCodecOptionLzmaWindowSize = matchArgs.ParamValue.Value;
						PrintLine(cmdinf, "LZMA Window Size      : " + cmdinf.CompressCodecOptionLzmaWindowSize);
						goto IL_0a39;
					}
				}
				ErrorExit(cmdinf, "LZMA_WINDOWSIZE out of range [0..7]");
			}
			goto IL_0a39;
		}
		goto IL_1553;
		IL_1553:
		PrintLine(cmdinf, "Data Alignment        : " + cmdinf.Alignment);
		PrintLine(cmdinf, "Directories info mask : " + cmdinf.Mask);
		PrintLine(cmdinf, "Text Code (csv)       : " + CAnalyCsvFile.GetCodeString(cmdinf.TextCode));
		if (cmdinf.ForceCompress)
		{
			PrintLine(cmdinf, "Force Compression     : " + cmdinf.ForceCompress);
		}
		if (cmdinf.UncompFilter != null)
		{
			PrintLine(cmdinf, "Uncompress File Ext.  : " + cmdinf.UncompFilter);
		}
		if (!cmdinf.EnableDateTimeInfo)
		{
			PrintLine(cmdinf, "DateTime Information  : false");
		}
		if (File.Exists(cmdinf.CsvFilename))
		{
			PrintLine(cmdinf, "Input Filename (csv)  : " + cmdinf.CsvFilename);
		}
		else if (Directory.Exists(cmdinf.CsvFilename))
		{
			PrintLine(cmdinf, "Input Directory       : " + cmdinf.CsvFilename);
			cmdinf.CsvFilename = cmdinf.CsvFilename.Trim(new char[1] { '/' });
			cmdinf.CsvFilename = cmdinf.CsvFilename.Trim(new char[1] { '\\' });
		}
		if (cmdinf.LogFilename != null)
		{
			PrintLine(cmdinf, "Input Access Log File : " + cmdinf.LogFilename);
			PrintLine(cmdinf, "Log Sort Calcs.       : " + cmdinf.LogSortCalcs);
			cmdinf.NoGroupSort = true;
			if (!File.Exists(cmdinf.LogFilename))
			{
				ErrorExit(cmdinf, "not exsists a log file.");
			}
			if (cmdinf.Mode == CommandInfo.EnumDataMode.ModeId)
			{
				ErrorExit(cmdinf, "Invalid mode. (-logfile, mode=ID)");
			}
		}
		if (cmdinf.CRC)
		{
			if (cmdinf.platform == CommandInfo.EnumTypePlatform.GP2)
			{
				PrintLine(cmdinf, "CheckSum Information  : True");
			}
			else
			{
				PrintLine(cmdinf, "CRC Information       : True");
			}
		}
		if (cmdinf.RandomPadding)
		{
			PrintLine(cmdinf, "Random Data Padding   : True");
		}
		if (cmdinf.DisableLocalInfo)
		{
			PrintLine(cmdinf, "No Local File Info.   : True");
		}
		if (cmdinf.ForceFileUnification)
		{
			PrintLine(cmdinf, "File Unification      : True");
		}
		if (cmdinf.ExcludedFilter != null)
		{
			PrintLine(cmdinf, "Excluded Filter       : " + cmdinf.ExcludedFilter);
		}
		if (cmdinf.EnableAfs2)
		{
			cmdinf.CpkFilename = Path.ChangeExtension(cmdinf.CpkFilename, ".afs2");
			PrintLine(cmdinf, "Enable AFS2 Format    : True");
		}
		if (cmdinf.CompFileAlign > 0)
		{
			PrintLine(cmdinf, "Compressed File Align : " + cmdinf.CompFileAlign);
		}
		if (cmdinf.Duplicate)
		{
			switch (cmdinf.Mode)
			{
			case CommandInfo.EnumDataMode.ModeId:
			case CommandInfo.EnumDataMode.ModeFilename:
			case CommandInfo.EnumDataMode.ModeFilenameAndId:
				ErrorExit(cmdinf, "Invalid mode. (-duplicate, not group)");
				break;
			case CommandInfo.EnumDataMode.ModeFilenameAndGroup:
			case CommandInfo.EnumDataMode.ModeIdAndGroup:
			case CommandInfo.EnumDataMode.ModeFilenameIdGroup:
				PrintLine(cmdinf, "Duplicates Group file : " + cmdinf.Duplicate);
				break;
			}
			cmdinf.NoGroupSort = true;
		}
		if (cmdinf.EnableMself)
		{
			string text3 = Path.GetExtension(cmdinf.CpkFilename).ToLower();
			if (text3 != ".mself")
			{
				cmdinf.CpkFilename += ".mself";
			}
		}
		PrintLine(cmdinf, "Output Filename (cpk) : " + cmdinf.CpkFilename);
		if (cmdinf.SafeMode)
		{
			PrintLine(cmdinf, "### Safe mode ###");
		}
		if (cmdinf.EnableAdditionalWriting && !File.Exists(cmdinf.CpkFilename))
		{
			PrintLine(cmdinf, "### Warning : Additional CPK file not found.");
			cmdinf.EnableAdditionalWriting = false;
		}
		if (!string.IsNullOrEmpty(cmdinf.AttrListFilename))
		{
			if (!File.Exists(cmdinf.AttrListFilename))
			{
				ErrorExit(cmdinf, "Not found a file. " + cmdinf.AttrListFilename);
			}
			using CAnalyCsvFile cAnalyCsvFile = new CAnalyCsvFile();
			cmdinf.AttrListAlignment = new Dictionary<string, int>();
			if (!cAnalyCsvFile.AnalizeCsvFile(cmdinf.AttrListFilename, cmdinf.TextCode))
			{
				ErrorExit(cmdinf, "Failed to analyze CSV file. " + cmdinf.AttrListFilename);
			}
			CMkAttrInfo cMkAttrInfo = new CMkAttrInfo();
			while (true)
			{
				bool flag2 = true;
				if (!cAnalyCsvFile.GetLine(cMkAttrInfo))
				{
					break;
				}
				if (cmdinf.AttrListAlignment.ContainsKey(cMkAttrInfo.AttributeName))
				{
					ErrorExit(cmdinf, "Invalid CSV file in line." + cMkAttrInfo.Lines + " " + cmdinf.AttrListFilename);
				}
				cmdinf.AttrListAlignment.Add(cMkAttrInfo.AttributeName, cMkAttrInfo.Alignment);
			}
		}
		ArgsCmdInfo invalidCommand = cAnalyCmdArgs.GetInvalidCommand();
		if (invalidCommand != null)
		{
			ErrorExit(cmdinf, $"Invalid Option \"-{invalidCommand.Command}\"");
		}
		if (cmdinf.CompFileAlign > 0 && cmdinf.Mode == CommandInfo.EnumDataMode.ModeId && !cmdinf.Verify)
		{
			ErrorExit(cmdinf, "Cannot use to mode of ID Only and enabled Compress File Alignment.");
		}
		return true;
		IL_0aeb:
		if (((matchArgs = cAnalyCmdArgs.GetMatchArgs("RELC_PAGESIZE")) != null || (matchArgs = cAnalyCmdArgs.GetMatchArgs("RELCPAGESIZE")) != null) && matchArgs.ParamValue.HasValue)
		{
			int? paramValue = matchArgs.ParamValue;
			if (paramValue.GetValueOrDefault() >= 0 && paramValue.HasValue && matchArgs.ParamValue <= 31)
			{
				cmdinf.CompressCodecOptionRelcPageSize = matchArgs.ParamValue.Value;
				PrintLine(cmdinf, "RELC Page Size        : " + cmdinf.CompressCodecOptionRelcPageSize);
			}
		}
		if (((matchArgs = cAnalyCmdArgs.GetMatchArgs("RELC_ROMTYPE")) != null || (matchArgs = cAnalyCmdArgs.GetMatchArgs("RELCROMTYPE")) != null) && matchArgs.ParamValue.HasValue)
		{
			int? paramValue = matchArgs.ParamValue;
			if (paramValue.GetValueOrDefault() >= 0 && paramValue.HasValue && matchArgs.ParamValue <= 1)
			{
				cmdinf.CompressCodecOptionRelcRomType = matchArgs.ParamValue.Value;
				PrintLine(cmdinf, "RELC Rom Type         : " + cmdinf.CompressCodecOptionRelcRomType);
			}
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("NOLOCALINFO")) != null)
		{
			cmdinf.DisableLocalInfo = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("VIEW")) != null)
		{
			cmdinf.Display = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("MASK")) != null)
		{
			cmdinf.Mask = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("DUPLICATE")) != null)
		{
			cmdinf.Duplicate = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("CRC")) != null)
		{
			cmdinf.CRC = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("CHECKSUM")) != null)
		{
			cmdinf.CRC = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("NOCHECKSUM")) != null && cmdinf.platform == CommandInfo.EnumTypePlatform.GP2)
		{
			cmdinf.CRC = false;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("RAND")) != null)
		{
			if (cmdinf.platform == CommandInfo.EnumTypePlatform.GP2)
			{
				ErrorExit(cmdinf, "-rand option is not spproted for this target.");
			}
			else
			{
				cmdinf.RandomPadding = true;
			}
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("FORCECOMPRESS")) != null)
		{
			cmdinf.ForceCompress = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("NODATETIME")) != null)
		{
			cmdinf.EnableDateTimeInfo = false;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("SHORTDEFINE")) != null || (matchArgs = cAnalyCmdArgs.GetMatchArgs("SDEF")) != null)
		{
			cmdinf.DefineHeaderShort = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("FILEUNIFICATION")) != null || (matchArgs = cAnalyCmdArgs.GetMatchArgs("FU")) != null)
		{
			cmdinf.ForceFileUnification = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("GINF")) != null)
		{
			cmdinf.EnableGinf = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("NOGINF")) != null)
		{
			cmdinf.EnableGinf = false;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("DIR")) != null)
		{
			if (matchArgs.ParamString != null)
			{
				if (Directory.Exists(matchArgs.ParamString))
				{
					cmdinf.OriginalCurrentDir = Directory.GetCurrentDirectory();
					Directory.SetCurrentDirectory(matchArgs.ParamString);
				}
				else
				{
					ErrorExit(cmdinf, CFileUtil.AddDoubleQuote(matchArgs.ParamString) + "is not found.");
				}
			}
			PrintLine(cmdinf, "Output Header         : " + cmdinf.HeaderFile);
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("NOTOPTOCINFO")) != null)
		{
			PrintLine(cmdinf, "No Top TOC info.      : True");
			cmdinf.EnableTopTocInfo = false;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("ADD")) != null)
		{
			if (cmdinf.Mode == CommandInfo.EnumDataMode.ModeId)
			{
				ErrorExit(cmdinf, "Invalid mode. (-add, mode=id)");
			}
			if (cmdinf.Mode == CommandInfo.EnumDataMode.ModeIdAndGroup)
			{
				ErrorExit(cmdinf, "Invalid mode. (-add, mode=idgroup)");
			}
			if (cmdinf.DisableLocalInfo)
			{
				ErrorExit(cmdinf, "Invalid mode. (-add, -nolocalinfo)");
			}
			if (!cmdinf.EnableDateTimeInfo)
			{
				ErrorExit(cmdinf, "Invalid mode. (-add, -nodatetime)");
			}
			PrintLine(cmdinf, "Additional Build      : True");
			cmdinf.EnableAdditionalWriting = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("ADDRANGE")) != null && matchArgs.ParamString != null)
		{
			cmdinf.AddRangeCsvFname = matchArgs.ParamString;
			PrintLine(cmdinf, "Add build update range info CSV Filename   : " + cmdinf.AddRangeCsvFname);
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("GROUPDATAALIGNMENT")) != null || (matchArgs = cAnalyCmdArgs.GetMatchArgs("GROUPALIGN")) != null)
		{
			if (matchArgs.ParamValue.HasValue && matchArgs.ParamValue > 0)
			{
				cmdinf.GroupDataAlignment = matchArgs.ParamValue.Value;
				PrintLine(cmdinf, "Group Data Alignment  : " + cmdinf.GroupDataAlignment);
			}
			else
			{
				ErrorExit(cmdinf, "Invalid group data alignment.");
			}
		}
		if (((matchArgs = cAnalyCmdArgs.GetMatchArgs("ATTRIBUTEALIGNMRNT")) != null || (matchArgs = cAnalyCmdArgs.GetMatchArgs("ATTRALIGN")) != null) && matchArgs.ParamString != null)
		{
			cmdinf.AttrListFilename = matchArgs.ParamString;
			PrintLine(cmdinf, "Attribute Align File  : " + matchArgs.ParamString);
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("VERIFY")) != null)
		{
			PrintLine(cmdinf, "Verify Mode           : Enabled");
			if (!File.Exists(cmdinf.CpkFilename))
			{
				ErrorExit(cmdinf, "Verify CPK file is not exists.");
			}
			cmdinf.Verify = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("BUILDANDVERIFY")) != null)
		{
			PrintLine(cmdinf, "Build and Verify      : Enabled");
			cmdinf.BuildAndVerify = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("TARGETGROUP")) != null)
		{
			cmdinf.TargetGroup = matchArgs.ParamString;
			PrintLine(cmdinf, "Target Group          : " + cmdinf.TargetGroup);
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("COMMENT")) != null)
		{
			cmdinf.Comment = matchArgs.ParamString;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("DISPMODE")) != null)
		{
			if (matchArgs.IsMatchParam("OFF"))
			{
				cmdinf.DispMode = CommandInfo.EnumDispMode.Off;
			}
			else if (matchArgs.IsMatchParam("NOPROGRESS"))
			{
				cmdinf.DispMode = CommandInfo.EnumDispMode.NoProgress;
			}
			else
			{
				cmdinf.DispMode = CommandInfo.EnumDispMode.All;
			}
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("DIVIDEDSIZE")) != null)
		{
			if (cmdinf.CompressCodec != 0)
			{
				ErrorExit(cmdinf, "DPK file mode cannot be used except a Layla codec.");
			}
			if (matchArgs.ParamValue.HasValue && matchArgs.ParamValue > 0)
			{
				cmdinf.DividedSizeKByte = matchArgs.ParamValue.Value;
				PrintLine(cmdinf, "Divided Size          : " + cmdinf.DividedSizeKByte + " KB");
			}
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("LOGSORT")) != null)
		{
			if (matchArgs.ParamString == null)
			{
				ErrorExit(cmdinf, "not exsists a log file.");
			}
			else
			{
				cmdinf.LogFilename = matchArgs.ParamString;
			}
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("MSELF")) != null)
		{
			cmdinf.EnableMself = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("ETERNALLOOP")) != null)
		{
			cmdinf.EternalLoop = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("LOCALPATHDEFINE")) != null || (matchArgs = cAnalyCmdArgs.GetMatchArgs("LPDDEF")) != null)
		{
			cmdinf.DefineHeaderLocalFilePath = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("NOGROUPSORT")) != null)
		{
			cmdinf.NoGroupSort = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("CSVORDERDEFINE")) != null || (matchArgs = cAnalyCmdArgs.GetMatchArgs("CSVDEF")) != null)
		{
			cmdinf.DefineHeaderCsvDefOrder = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("AFS2")) != null)
		{
			cmdinf.EnableAfs2 = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("UNCOMPFILEEXT")) != null && matchArgs.ParamString != null)
		{
			cmdinf.UncompFilter = matchArgs.ParamString;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("CPKPROJ")) != null)
		{
			if (matchArgs.ParamString == null || matchArgs.ParamString.Length == 0)
			{
				cmdinf.CpkprojFilename = CFileUtil.ChangeExtention(cmdinf.CpkFilename, ".cpkproj");
			}
			else
			{
				cmdinf.CpkprojFilename = matchArgs.ParamString;
			}
			PrintLine(cmdinf, "Output Cpkproj        : " + cmdinf.CpkprojFilename);
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("ATTRSTRING")) != null)
		{
			cmdinf.EnableAttributeUserString = true;
			PrintLine(cmdinf, "User Attribute String : Enabled");
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("SAFEMODE")) != null)
		{
			cmdinf.SafeMode = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("UNFIXEDMEMORY")) != null)
		{
			cmdinf.UseUnfixedMemory = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("DEBUG")) != null)
		{
			cmdinf.Debug = true;
		}
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("TEST")) != null)
		{
			cmdinf.Test = true;
		}
		goto IL_1553;
		IL_0a39:
		if ((matchArgs = cAnalyCmdArgs.GetMatchArgs("LZMA_BLOCKSIZE")) != null)
		{
			if (matchArgs.ParamValue.HasValue)
			{
				int? paramValue = matchArgs.ParamValue;
				if (paramValue.GetValueOrDefault() >= 10 && paramValue.HasValue && matchArgs.ParamValue <= 31)
				{
					cmdinf.CompressCodecOptionLzmaBlockSize = matchArgs.ParamValue.Value;
					PrintLine(cmdinf, "LZMA Block Size       : " + cmdinf.CompressCodecOptionLzmaBlockSize);
					goto IL_0aeb;
				}
			}
			ErrorExit(cmdinf, "LZMA_BLOCKSIZE out of range [10..31]");
		}
		goto IL_0aeb;
	}

	private static int Verify(CommandInfo cmdinf, List<CMkFileInfo> finfs)
	{
		using CpkMaker cpkMaker = new CpkMaker();
		bool flag = cpkMaker.AnalyzeCpkFile(cmdinf.CpkFilename);
		if (cpkMaker.EnableItoc)
		{
			Dictionary<uint, string> dictionary = new Dictionary<uint, string>();
			foreach (CMkFileInfo finf in finfs)
			{
				if (!dictionary.ContainsKey(finf.FileId))
				{
					dictionary.Add(finf.FileId, finf.LocalFilePath);
				}
			}
			foreach (CFileInfo fileInfo in cpkMaker.FileData.FileInfos)
			{
				if (dictionary.ContainsKey(fileInfo.FileId))
				{
					fileInfo.LocalFilePath = dictionary[fileInfo.FileId];
				}
				else
				{
					ErrorExit(cmdinf, "Internal error : Local file not found. ID = " + fileInfo.FileId);
				}
			}
		}
		else if (cpkMaker.EnableToc)
		{
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
			foreach (CMkFileInfo finf2 in finfs)
			{
				string key = finf2.ContentFilePath.Replace('\\', '/').Trim(new char[1] { '/' });
				if (!dictionary2.ContainsKey(key))
				{
					dictionary2.Add(key, finf2.LocalFilePath);
				}
			}
			foreach (CFileInfo fileInfo2 in cpkMaker.FileData.FileInfos)
			{
				if (dictionary2.ContainsKey(fileInfo2.ContentFilePath))
				{
					fileInfo2.LocalFilePath = dictionary2[fileInfo2.ContentFilePath];
				}
				else
				{
					ErrorExit(cmdinf, "Internal error : Local file not found. Contents Name = " + fileInfo2.ContentFilename);
				}
			}
		}
		string tempPath = Path.GetTempPath();
		cpkMaker.StartToVerify(cmdinf.CpkFilename, tempPath);
		PrintLine(cmdinf, "Verifying \"" + cmdinf.CpkFilename + "\" ... ");
		ulong num = 0uL;
		while (true)
		{
			bool flag2 = true;
			Status status = cpkMaker.Execute();
			if (status == Status.Error)
			{
				ErrorExit(cmdinf, cpkMaker.ErrorMessage);
			}
			double progress = cpkMaker.GetProgress();
			PrintfProgress(cmdinf, progress.ToString("F2").PadLeft(7) + "% " + MakeProgressString(progress) + "\r");
			if (status == Status.Complete)
			{
				break;
			}
			num++;
		}
		PrintLine(cmdinf, "\r\ncompleted.\r\n");
		return 0;
	}

	private static void ShowUsages(string[] args, CommandInfo cmdinf)
	{
		ConsoleKeyInfo consoleKeyInfo = default(ConsoleKeyInfo);
		PrintLine(cmdinf, "|||||||||||||||| " + CFileUtil.GetMyFileVersionWithProductString() + " ||||||||||||||||");
		Printf(cmdinf, CFileUtil.GetMyCopyrightString());
		PrintLine(cmdinf, "      (CpkMaker.dll Ver." + CpkMaker.GetDllVersionString() + ")");
		PrintLine(cmdinf, "");
		if (args.Length < 1)
		{
			PrintLine(cmdinf, "  usage: cpkmakec <Input CSV File or Directory> <Output CPK File> [Option]");
			PrintLine(cmdinf, "");
			PrintLine(cmdinf, "[Options]");
			PrintLine(cmdinf, "  -dir=[Current Directory]  : Set current directory for working.");
			PrintLine(cmdinf, "  -align=[Alignment]        : Size of data alignment. default: -align=" + 2048);
			PrintLine(cmdinf, "  -code=[SJIS|EUC|UTF-8]    : Text code for CSV file. default: -code=SJIS");
			PrintLine(cmdinf, "  -mode=<MODE>              : CPK file mode.          default: -mode=ID");
			PrintLine(cmdinf, "                              ID              : ID Only (Compact)");
			PrintLine(cmdinf, "                              FILENAME        : Filename Only");
			PrintLine(cmdinf, "                              FILENAMEGROUP   : Filename and Group (Attribute)");
			PrintLine(cmdinf, "                              IDGROUP         : ID and Group");
			PrintLine(cmdinf, "                              FILENAMEID      : Filename and ID");
			PrintLine(cmdinf, "                              FILENAMEIDGROUP : Filename and ID, Group");
			PrintLine(cmdinf, "  -duplicate                : Duplicates the grouping files.");
			if (cmdinf.platform == CommandInfo.EnumTypePlatform.GP2)
			{
				PrintLine(cmdinf, "  -checksum                 : Enable the CheckSum information.");
				PrintLine(cmdinf, "                              (default : Enable)");
			}
			else
			{
				PrintLine(cmdinf, "  -crc                      : Enable the CRC information.");
			}
			if (cmdinf.platform == CommandInfo.EnumTypePlatform.GP2)
			{
				PrintLine(cmdinf, "  -nochecksum               : Disable the CheckSum information.");
			}
			PrintLine(cmdinf, "  -add                      : Try additional building. (excluded ID Only mode)");
			PrintLine(cmdinf, "  -header=[Header Filename] : Enable to output the header file (.h).");
			PrintLine(cmdinf, "  -mask                     : Enable to directries information mask.");
			if (cmdinf.platform != CommandInfo.EnumTypePlatform.GP2)
			{
				PrintLine(cmdinf, "  -rand                     : Enable to random data padding.");
			}
			PrintLine(cmdinf, "  -forcecompress            : Regardless of CSV file settings, Try compression.");
			PrintLine(cmdinf, "  -fileunification          : Enable to File unification.");
			PrintLine(cmdinf, "  -shortdefine              : Definition of the header only filename.(w/o ID)");
			PrintLine(cmdinf, "  -notoptocinfo             : Disable to Top TOC information.(to footer)");
			PrintLine(cmdinf, "  -nolocalinfo              : Disable to Local file information. (*)");
			PrintLine(cmdinf, "  -nodatetime               : Disable to DateTime information. (*)");
			PrintLine(cmdinf, "  -compper=[Percentage]     : Percentage of Compression threshold.");
			PrintLine(cmdinf, "  -compfilesize=[File size] : Original File Size for Compression threshold.");
			PrintLine(cmdinf, "  -groupalign=[Alignment]   : File alignment for Top of Group file.");
			PrintLine(cmdinf, "  -attralign=[Attr Filename]: File alignment for files by an Attribute.");
			PrintLine(cmdinf, "  -addrange=[CSV Fliename]  : Output Add build update range info[offset,size].");
			PrintLine(cmdinf, "  -excluded=[Wildcard]      : do not include the excluded file which matched ");
			PrintLine(cmdinf, "                              wild card.(Dir input only.) ex.\"*.svn;*.bak\"");
			PrintLine(cmdinf, "  -cpkproj=[Filename]       : Generate the cpkproj file for CPK File Builder.");
			PrintLine(cmdinf, "  -cpkroot=[Path]           : Append to top of CPK path.");
			PrintLine(cmdinf, "  -view                     : Show the CPK file information after building.");
			PrintLine(cmdinf, "  -noerrorstop              : No wait for key input when error occurred.");
			PrintLine(cmdinf, "  -attrstring               : Enable to tha Attribute name string each file.");
			if (cmdinf.platform == CommandInfo.EnumTypePlatform.GP2)
			{
				PrintLine(cmdinf, "  -lzma_windowsize=[0..7]   : Window size=(1<<(7+lzma_windowsize)). default:3");
				PrintLine(cmdinf, "  -lzma_blocksize=[10..31]  : Block size=(1<<lzma_blocksize). default:17=128KB");
				PrintLine(cmdinf, "                   Default lzma_blocksize setting is fit for almost ROM type.");
				PrintLine(cmdinf, "                   Please read tools manual if you change lzma_blocksize.");
			}
			if (cmdinf.platform == CommandInfo.EnumTypePlatform.FRV)
			{
				PrintLine(cmdinf, "  -relc_pagesize=[0..31]    : Page Size=(1<<relc_pagesize).   default:12");
				PrintLine(cmdinf, "  -relc_romtype=[0|1]       : 1:NAND32, 0:Other.              default:1");
				PrintLine(cmdinf, "  -outcpkh                  : Output .cpkh file(Header part of .cpk file). ");
			PrintLine(cmdinf, "  --ffbejp                  : FFBE JP format. Encrypt each file with a key derived from its filename.");
			PrintLine(cmdinf, "                              Use a mode that stores file names (not ID). No compression.");
			}
			PrintLine(cmdinf, "");
			PrintLine(cmdinf, "  (*) Cannot Additional build");
			PrintLine(cmdinf, "");
			PrintLine(cmdinf, "* Viewing mode - Viewing of the CPK file information.");
			PrintLine(cmdinf, "  usage: cpkmakec <Input CPK File>");
			PrintLine(cmdinf, "[Options]");
			PrintLine(cmdinf, "  -exportcpkinfo=[CSV Fliename] : Export a CPK File information.");
			PrintLine(cmdinf, "  -extract=[outdir]             : Extract content files.");
			PrintLine(cmdinf, "  --ffbejp                      : FFBE JP format. Decrypt the extracted files.");
			PrintLine(cmdinf, "");
			PrintLine(cmdinf, "* Verify mode - Compare a built CPK and Local original files.");
			PrintLine(cmdinf, "  usage: cpkmakec  <Input CSV File or Directory> <Input CPK File> -verify");
			PrintLine(cmdinf, "");
			PrintLine(cmdinf, "* Input CSV File format example");
			PrintLine(cmdinf, "  <Local Filename>, [Contents Filename], [ID], [Compress/Uncompress]");
			PrintLine(cmdinf, "  <Local Filename>, [Contents Filename], [ID], [C/UC], [Groups], [Attribute]");
			if (consoleKeyInfo.Modifiers == ConsoleModifiers.Shift)
			{
				PrintLine(cmdinf, "");
				PrintLine(cmdinf, "  ======== unofficial commands ========================================");
				PrintLine(cmdinf, "  -mself                    : Enable to the MSELF format file.");
				PrintLine(cmdinf, "  -ginf                     : Enable to the Ginf.");
				PrintLine(cmdinf, "  -noginf                   : Disable to the Ginf.");
				PrintLine(cmdinf, "  -dispmode=[mode]          : OFF|NOPROGRESS|*ON");
				PrintLine(cmdinf, "  -afs2                     : AFS2 mode.");
				PrintLine(cmdinf, "  -uncompfileext=[Wildcard] : Uncompression file extension. *.cpk;*.avi;");
				PrintLine(cmdinf, "  -dividedsize=[divsize]    : for DPK file mode. [KB]");
				PrintLine(cmdinf, "  -compfilealign=[align]    : Compressed file alignment. [Byte]");
				PrintLine(cmdinf, "  -logsort=[logfilename]    : for GA Sort.");
				PrintLine(cmdinf, "  -calcs=[calcs]            : GA number of calculation.");
				PrintLine(cmdinf, "  -nogroupsort              : No group sorting.");
				PrintLine(cmdinf, "  -targetgroup=[Geoup name] : Use only an appointed group.");
				PrintLine(cmdinf, "");
				PrintLine(cmdinf, "  -comment=[comment]        : Comment");
				PrintLine(cmdinf, "  -safemode                 : Switch to Sync packing mode.");
				PrintLine(cmdinf, "  -unfixedmemory            : Unfixed memory mode.");
				PrintLine(cmdinf, "  -debug                    : Debug mode.");
				PrintLine(cmdinf, "  -test                     : Test mode.");
			}
			Environment.Exit(0);
		}
	}

	private static void CheckDllRemainDays()
	{
		string dllRemainDays365String = CpkMaker.GetDllRemainDays365String();
		if (dllRemainDays365String != null)
		{
			Console.WriteLine(dllRemainDays365String);
			Environment.Exit(1);
		}
	}

	private static bool IsFileMajikPro()
	{
		if (File.Exists("CpkMakerControl.dll"))
		{
			try
			{
				FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo("CpkMakerControl.dll");
				if (versionInfo.FileVersion != null)
				{
					return true;
				}
			}
			catch (Exception)
			{
				return false;
			}
		}
		return false;
	}

	private static CpkMaker.EnumCpkFileMode ConvertCpkFileMode(CommandInfo cmdinf)
	{
		return cmdinf.Mode switch
		{
			CommandInfo.EnumDataMode.ModeId => CpkMaker.EnumCpkFileMode.ModeId, 
			CommandInfo.EnumDataMode.ModeFilename => CpkMaker.EnumCpkFileMode.ModeFilename, 
			CommandInfo.EnumDataMode.ModeFilenameAndId => CpkMaker.EnumCpkFileMode.ModeFilenameAndId, 
			CommandInfo.EnumDataMode.ModeFilenameAndGroup => CpkMaker.EnumCpkFileMode.ModeFilenameAndGroup, 
			CommandInfo.EnumDataMode.ModeIdAndGroup => CpkMaker.EnumCpkFileMode.ModeIdAndGroup, 
			CommandInfo.EnumDataMode.ModeFilenameIdGroup => CpkMaker.EnumCpkFileMode.ModeFilenameIdGroup, 
			_ => throw new Exception("Unknown mode"), 
		};
	}

	private static bool CheckAdditionalFile(CommandInfo cmdinf, List<CMkFileInfo> finfs, CpkMaker maker)
	{
		Console.Write("CPK File Checking ... ");
		if (!maker.AnalyzeCpkFile(cmdinf.CpkFilename) || finfs == null)
		{
			return false;
		}
		switch (maker.TestConsistency(ConvertCpkFileMode(cmdinf), (uint)cmdinf.Alignment, cmdinf.GroupDataAlignment))
		{
		case -1:
			ErrorExit(cmdinf, "Invalid cpk file mode setting.");
			break;
		case -2:
			ErrorExit(cmdinf, "Invalid alignment setting.");
			break;
		}
		CheckCpkConpatibleAdd(cmdinf, maker);
		int count = maker.FileData.FileInfos.Count;
		foreach (CFileInfo fileInfo in maker.FileData.FileInfos)
		{
			fileInfo.Tag = null;
		}
		foreach (CMkFileInfo finf in finfs)
		{
			finf.ContentFilePath = finf.ContentFilePath.Replace('\\', '/').Trim(new char[1] { '/' });
		}
		if (finfs != null)
		{
			List<CFileInfo> fileInfos = maker.FileData.FileInfos;
			foreach (CMkFileInfo finf2 in finfs)
			{
				foreach (CFileInfo item in fileInfos)
				{
					if (finf2.ContentFilePath.Equals(item.ContentFilePath) && IsMatchFileInfo(finf2, item, cmdinf.EnableGroupAttribute))
					{
						if (finf2.CompressAdd)
						{
							finf2.Compress = item.IsCompressed;
						}
						item.Tag = finf2;
						finf2.Tag = item;
						break;
					}
				}
			}
		}
		maker.FileData.RemoveNoTagFileInfos();
		count -= maker.FileData.FileInfos.Count;
		Console.WriteLine("done.");
		Console.WriteLine("Remove file(s) : " + count.ToString("N0"));
		return true;
	}

	private static void CheckCpkConpatibleAdd(CommandInfo cmdinf, CpkMaker maker)
	{
		if (maker.CpkFileMode == CpkMaker.EnumCpkFileMode.ModeId)
		{
			ErrorExit(cmdinf, "-add is not compatible with the input CPK(mode=id).");
		}
		if (maker.CpkFileMode == CpkMaker.EnumCpkFileMode.ModeIdAndGroup)
		{
			ErrorExit(cmdinf, "-add is not compatible with the input CPK(mode=idgroup).");
		}
		if (maker.CpkDateTime64 == 0)
		{
			ErrorExit(cmdinf, "-add is not compatible with the input CPK(nodatetime).");
		}
		if (!maker.EnableEtoc)
		{
			ErrorExit(cmdinf, "-add is not compatible with the input CPK(nolocalinfo).");
		}
	}

	private static void PrintLine(CommandInfo cmdinf, string str)
	{
		if (cmdinf.DispMode == CommandInfo.EnumDispMode.All || cmdinf.DispMode == CommandInfo.EnumDispMode.NoProgress)
		{
			Console.WriteLine(str);
		}
	}

	private static void Printf(CommandInfo cmdinf, string str)
	{
		if (cmdinf.DispMode == CommandInfo.EnumDispMode.All || cmdinf.DispMode == CommandInfo.EnumDispMode.NoProgress)
		{
			Console.Write(str);
		}
	}

	private static void PrintfProgress(CommandInfo cmdinf, string str)
	{
		if (cmdinf.DispMode == CommandInfo.EnumDispMode.All)
		{
			Console.Write(str);
		}
	}

	private static string GetMyExecutingFilename()
	{
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		return executingAssembly.Location;
	}

	[Conditional("DEBUG")]
	private static void DebugTest()
	{
		CMkFileInfo cMkFileInfo = new CMkFileInfo();
		CAnalyCsvFile cAnalyCsvFile = new CAnalyCsvFile();
		Directory.CreateDirectory("dir");
		StreamWriter streamWriter = new StreamWriter("dir/cri_csv_test.csv");
		streamWriter.WriteLine("dir/cri_csv_test.csv, 1234.dat, 4567, Compress");
		streamWriter.WriteLine("dir/cri_csv_test.csv, 1235.dat, 4568, Uncompress");
		streamWriter.Close();
		streamWriter.Dispose();
		cAnalyCsvFile.AnalizeCsvFile("dir/cri_csv_test.csv", CAnalyCsvFile.TextCode.CODE_UTF8);
		cAnalyCsvFile.GetLine(cMkFileInfo);
		if (cMkFileInfo.LocalFilePath.Replace('/', '\\') != "dir\\cri_csv_test.csv" || cMkFileInfo.ContentFilePath != "1234.dat" || cMkFileInfo.FileId != 4567 || !cMkFileInfo.Compress)
		{
			throw new Exception("CSV Error!!");
		}
		cAnalyCsvFile.GetLine(cMkFileInfo);
		if (cMkFileInfo.LocalFilePath.Replace('/', '\\') != "dir\\cri_csv_test.csv" || cMkFileInfo.ContentFilePath != "1235.dat" || cMkFileInfo.FileId != 4568 || cMkFileInfo.Compress)
		{
			throw new Exception("CSV Error!!");
		}
		cAnalyCsvFile.Dispose();
		streamWriter = new StreamWriter("dir\\cri_csv_test.csv");
		streamWriter.WriteLine("dir\\cri_csv_test.csv, 1234.dat, 4567, Compress");
		streamWriter.WriteLine("dir\\cri_csv_test.csv, 1235.dat, 4568, Uncompress");
		streamWriter.Close();
		streamWriter.Dispose();
		cAnalyCsvFile.AnalizeCsvFile("dir\\cri_csv_test.csv", CAnalyCsvFile.TextCode.CODE_UTF8);
		cAnalyCsvFile.GetLine(cMkFileInfo);
		if (cMkFileInfo.LocalFilePath.Replace('/', '\\') != "dir\\cri_csv_test.csv" || cMkFileInfo.ContentFilePath != "1234.dat" || cMkFileInfo.FileId != 4567 || !cMkFileInfo.Compress)
		{
			throw new Exception("CSV Error!!");
		}
		cAnalyCsvFile.GetLine(cMkFileInfo);
		if (cMkFileInfo.LocalFilePath.Replace('/', '\\') != "dir\\cri_csv_test.csv" || cMkFileInfo.ContentFilePath != "1235.dat" || cMkFileInfo.FileId != 4568 || cMkFileInfo.Compress)
		{
			throw new Exception("CSV Error!!");
		}
		cAnalyCsvFile.Dispose();
		Directory.CreateDirectory("c:\\dir");
		streamWriter = new StreamWriter("c:\\dir\\cri_csv_test.csv");
		streamWriter.WriteLine("c:\\dir\\cri_csv_test.csv, 1234.dat, 4567, Compress");
		streamWriter.WriteLine("c:\\dir\\cri_csv_test.csv, 1235.dat, 4568, Uncompress");
		streamWriter.Close();
		streamWriter.Dispose();
		cAnalyCsvFile.AnalizeCsvFile("c:\\dir\\cri_csv_test.csv", CAnalyCsvFile.TextCode.CODE_UTF8);
		cAnalyCsvFile.GetLine(cMkFileInfo);
		if (cMkFileInfo.LocalFilePath != "c:\\dir\\cri_csv_test.csv" || cMkFileInfo.ContentFilePath != "1234.dat" || cMkFileInfo.FileId != 4567 || !cMkFileInfo.Compress)
		{
			throw new Exception("CSV Error!!");
		}
		cAnalyCsvFile.GetLine(cMkFileInfo);
		if (cMkFileInfo.LocalFilePath != "c:\\dir\\cri_csv_test.csv" || cMkFileInfo.ContentFilePath != "1235.dat" || cMkFileInfo.FileId != 4568 || cMkFileInfo.Compress)
		{
			throw new Exception("CSV Error!!");
		}
		cAnalyCsvFile.Dispose();
		File.Delete("c:\\dir\\cri_csv_test.csv");
		Directory.Delete("c:\\dir");
		streamWriter = new StreamWriter("cri_csv_test.csv");
		streamWriter.WriteLine("cri_csv_test.csv, , 4567, Compress");
		streamWriter.WriteLine("cri_csv_test.csv, , 4568, Uncompress");
		streamWriter.Close();
		streamWriter.Dispose();
		cAnalyCsvFile.AnalizeCsvFile("cri_csv_test.csv", CAnalyCsvFile.TextCode.CODE_UTF8);
		cAnalyCsvFile.GetLine(cMkFileInfo);
		if (cMkFileInfo.LocalFilePath != "cri_csv_test.csv" || cMkFileInfo.ContentFilePath != "cri_csv_test.csv" || cMkFileInfo.FileId != 4567 || !cMkFileInfo.Compress || !string.IsNullOrEmpty(cMkFileInfo.Attribute) || !string.IsNullOrEmpty(cMkFileInfo.Groups))
		{
			throw new Exception("CSV Error!!");
		}
		cAnalyCsvFile.GetLine(cMkFileInfo);
		if (cMkFileInfo.LocalFilePath != "cri_csv_test.csv" || cMkFileInfo.ContentFilePath != "cri_csv_test.csv" || cMkFileInfo.FileId != 4568 || cMkFileInfo.Compress || !string.IsNullOrEmpty(cMkFileInfo.Attribute) || !string.IsNullOrEmpty(cMkFileInfo.Groups))
		{
			throw new Exception("CSV Error!!");
		}
		cAnalyCsvFile.Dispose();
		streamWriter = new StreamWriter("cri_csv_test.csv");
		streamWriter.WriteLine("cri_csv_test.csv, , , C");
		streamWriter.WriteLine("cri_csv_test.csv, , , UC");
		streamWriter.Close();
		streamWriter.Dispose();
		cAnalyCsvFile.AnalizeCsvFile("cri_csv_test.csv", CAnalyCsvFile.TextCode.CODE_UTF8);
		cAnalyCsvFile.GetLine(cMkFileInfo);
		if (cMkFileInfo.LocalFilePath != "cri_csv_test.csv" || cMkFileInfo.ContentFilePath != "cri_csv_test.csv" || cMkFileInfo.FileId != 0 || !cMkFileInfo.Compress || !string.IsNullOrEmpty(cMkFileInfo.Attribute) || !string.IsNullOrEmpty(cMkFileInfo.Groups))
		{
			throw new Exception("CSV Error!!");
		}
		cAnalyCsvFile.GetLine(cMkFileInfo);
		if (cMkFileInfo.LocalFilePath != "cri_csv_test.csv" || cMkFileInfo.ContentFilePath != "cri_csv_test.csv" || cMkFileInfo.FileId != 1 || cMkFileInfo.Compress || !string.IsNullOrEmpty(cMkFileInfo.Attribute) || !string.IsNullOrEmpty(cMkFileInfo.Groups))
		{
			throw new Exception("CSV Error!!");
		}
		cAnalyCsvFile.Dispose();
		streamWriter = new StreamWriter("cri_csv_test.csv");
		streamWriter.WriteLine("cri_csv_test.csv, , , C, Group1, Attr1");
		streamWriter.WriteLine("cri_csv_test.csv, , , UC, \"Group1, Group2\", Attr2");
		streamWriter.Close();
		streamWriter.Dispose();
		cAnalyCsvFile.AnalizeCsvFile("cri_csv_test.csv", CAnalyCsvFile.TextCode.CODE_UTF8);
		cAnalyCsvFile.GetLine(cMkFileInfo);
		if (cMkFileInfo.LocalFilePath != "cri_csv_test.csv" || cMkFileInfo.ContentFilePath != "cri_csv_test.csv" || cMkFileInfo.FileId != 0 || !cMkFileInfo.Compress || cMkFileInfo.Groups != "Group1" || cMkFileInfo.Attribute != "Attr1")
		{
			throw new Exception("CSV Error!!");
		}
		cAnalyCsvFile.GetLine(cMkFileInfo);
		if (cMkFileInfo.LocalFilePath != "cri_csv_test.csv" || cMkFileInfo.ContentFilePath != "cri_csv_test.csv" || cMkFileInfo.FileId != 1 || cMkFileInfo.Compress || cMkFileInfo.Groups != "Group1, Group2" || cMkFileInfo.Attribute != "Attr2")
		{
			throw new Exception("CSV Error!!");
		}
		cAnalyCsvFile.Dispose();
		File.Delete("cri_csv_test.csv");
		File.Delete("dir/cri_csv_test.csv");
		Directory.Delete("dir");
	}

	private static void testCallbackTest(string msg, int a)
	{
		Console.Write(msg);
	}

	private static bool testCpk(string fname, string paramstr)
	{
		string group = null;
		string attr = null;
		bool result = true;
		if (!string.IsNullOrEmpty(paramstr) && paramstr.Contains(","))
		{
			string[] array = paramstr.Split(new char[1] { ',' });
			if (array.Length == 1 || array.Length == 2)
			{
				group = array[0];
			}
			if (array.Length == 2)
			{
				attr = array[1];
			}
		}
		using (CpkMaker cpkMaker = new CpkMaker())
		{
			bool flag = cpkMaker.TestCpkFile(fname, testCallbackTest, group, attr);
			cpkMaker.AnalyzeCpkFile(fname);
			result = cpkMaker.TestCpkFileCompress(testCallbackTest);
		}
		return result;
	}

	private static int Main(string[] args)
	{
		try
		{
			IsFileMajikPro();
			CheckDllRemainDays();
			int num = 255;
			CommandInfo commandInfo = new CommandInfo();
			string text = GetMyExecutingFilename().ToLower();
			if (text.EndsWith("gp1.exe"))
			{
				commandInfo.platform = CommandInfo.EnumTypePlatform.GP1;
				commandInfo.CompressCodec = EnumCompressCodec.CodecLayla;
				commandInfo.EnableGinf = true;
			}
			else if (text.EndsWith("gp2.exe"))
			{
				commandInfo.platform = CommandInfo.EnumTypePlatform.GP2;
				commandInfo.CompressCodec = EnumCompressCodec.CodecLZMA;
				commandInfo.CompFileAlign = 8;
				commandInfo.EnableGinf = true;
				commandInfo.EnableHtocFpathToToc = true;
				commandInfo.LayoutOrderForCompAndUni = true;
			}
			else if (text.EndsWith("frv.exe"))
			{
				commandInfo.platform = CommandInfo.EnumTypePlatform.FRV;
				commandInfo.CompressCodec = EnumCompressCodec.CodecRELC;
				commandInfo.CompFileAlign = 4096;
				commandInfo.EnableGinf = true;
			}
			Console.Title = CFileUtil.GetMyProductString();
			long num2 = 0L;
			do
			{
				ShowUsages(args, commandInfo);
				AnalyCommandArgs(args, commandInfo);
				PreProcess(commandInfo);
				num = MakeByCsvFile(commandInfo);
				if (File.Exists(commandInfo.CpkFilename))
				{
					FileInfo fileInfo = new FileInfo(commandInfo.CpkFilename);
					if (num2 != 0 && num2 != fileInfo.Length)
					{
						Console.WriteLine("Illigal error");
					}
					num2 = fileInfo.Length;
				}
			}
			while (commandInfo.EternalLoop);
			if (commandInfo.CpkprojFilename != null)
			{
				ProcessStartInfo processStartInfo = new ProcessStartInfo();
				processStartInfo.FileName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\CpkFileBuilder.exe";
				processStartInfo.Arguments = "-MAKEPROJ";
				processStartInfo.Arguments = processStartInfo.Arguments + " \"" + Path.GetFullPath(commandInfo.CpkprojFilename) + "\"";
				processStartInfo.Arguments = processStartInfo.Arguments + " \"" + Path.GetFullPath(commandInfo.CsvFilename) + "\"";
				processStartInfo.Arguments = processStartInfo.Arguments + " \"" + Path.GetFullPath(commandInfo.CpkFilename) + "\"";
				processStartInfo.Arguments = processStartInfo.Arguments + " \"" + commandInfo.Alignment + "\"";
				string arguments = processStartInfo.Arguments;
				int mode = (int)commandInfo.Mode;
				processStartInfo.Arguments = arguments + " \"" + mode + "\"";
				processStartInfo.Arguments = processStartInfo.Arguments + " \"" + commandInfo.CRC + "\"";
				processStartInfo.Arguments = processStartInfo.Arguments + " \"" + commandInfo.HeaderFile + "\"";
				processStartInfo.Arguments = processStartInfo.Arguments + " \"" + commandInfo.Mask + "\"";
				processStartInfo.Arguments = processStartInfo.Arguments + " \"" + commandInfo.RandomPadding + "\"";
				processStartInfo.Arguments = processStartInfo.Arguments + " \"" + commandInfo.ForceCompress + "\"";
				processStartInfo.Arguments = processStartInfo.Arguments + " \"" + commandInfo.EnableTopTocInfo + "\"";
				processStartInfo.Arguments = processStartInfo.Arguments + " \"" + commandInfo.DisableLocalInfo + "\"";
				processStartInfo.Arguments = processStartInfo.Arguments + " \"" + !commandInfo.EnableDateTimeInfo + "\"";
				processStartInfo.Arguments = processStartInfo.Arguments + " \"" + commandInfo.CompPer + "\"";
				processStartInfo.Arguments = processStartInfo.Arguments + " \"" + commandInfo.CompFileSize + "\"";
				processStartInfo.Arguments = processStartInfo.Arguments + " \"" + commandInfo.GroupDataAlignment + "\"";
				Process process = Process.Start(processStartInfo);
				while (!process.HasExited)
				{
					Thread.Sleep(100);
				}
			}
			return num;
		}
		catch (Exception ex)
		{
			Console.WriteLine("ERROR Exception: " + ex.Message);
			return 1;
		}
	}
}
