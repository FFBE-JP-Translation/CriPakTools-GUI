using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LibCPK;
namespace CriPakTools
{
    class Program
    {
        static void PrintUsage()
        {
            Console.WriteLine("CRI CPK Tool");
            Console.WriteLine("Usage:");
            Console.WriteLine("  extract_all -p <cpk_file> -o <output_dir>   extract CPK all files to target output dir");
            Console.WriteLine("  replace -p <cpk_file> -i <patch_files_dir> -o <output_cpk> [-nc <optional: not compress>] [-nl <optional: nameless compress, ignore file suffix>]  replace patch files to CPK");
            Console.WriteLine("");
            Console.WriteLine("Demo:");
            Console.WriteLine("  CriPakTools.exe extract_all -p original.cpk -o extracted_files");
            Console.WriteLine("  CriPakTools.exe replace -p original.cpk -i modified_files -o modified.cpk [-nc]");
        }

        static CPK cpkContent = new CPK();
        static string cpkContentName = "";

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                PrintUsage();
                return;
            }

            string command = args[0].ToLower();

            try
            {
                switch (command)
                {
                    case "extract_all":
                        ExtractAllCommand(args);
                        break;
                    case "replace":
                        ReplaceCommand(args);
                        break;
                    default:
                        Console.WriteLine($"Unsupported: {command}");
                        PrintUsage();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        static void ExtractAllCommand(string[] args)
        {
            string cpkPath = "";
            string outputDir = "";

            // 解析参数
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "-p" && i + 1 < args.Length)
                {
                    cpkPath = args[i + 1];
                    i++;
                }
                else if (args[i] == "-o" && i + 1 < args.Length)
                {
                    outputDir = args[i + 1];
                    i++;
                }
            }

            if (string.IsNullOrEmpty(cpkPath) || string.IsNullOrEmpty(outputDir))
            {
                Console.WriteLine("Error: miss args");
                PrintUsage();
                return;
            }

            if (!File.Exists(cpkPath))
            {
                Console.WriteLine($"Error: CPK file not exist => {cpkPath}");
                return;
            }

            Console.WriteLine($"Start extract CPK: {cpkPath}");
            Console.WriteLine($"Ouput dir: {outputDir}");

            ExtractAll(cpkPath, outputDir);

            Console.WriteLine("\rExtract finished!");
        }

        static bool CheckDuplicateIds(List<FileEntry> entries)
        {
            HashSet<int> ids = new HashSet<int>();

            foreach (FileEntry entry in entries)
            {
                if (entry.ID != null)
                {
                    if (ids.Contains(Convert.ToInt32(entry.ID)))
                    {
                        return true;
                    }

                    ids.Add(Convert.ToInt32(entry.ID));
                }
            }

            return false;
        }

        private static string DetectFileExtension(byte[] fileHeader, long fileSize)
        {
            if (fileHeader == null || fileHeader.Length < 4)
                return "";

            byte[] magic = new byte[4];
            Array.Copy(fileHeader, magic, 4);

            // Check vtx\x00
            if (magic[0] == 0x76 && magic[1] == 0x74 && magic[2] == 0x78 && magic[3] == 0x00)
                return ".vtx";

            // Check xtx\x00
            if (magic[0] == 0x78 && magic[1] == 0x74 && magic[2] == 0x78 && magic[3] == 0x00)
                return ".xtx";

            try
            {
                int offset = BitConverter.ToInt32(magic, 0);

                if (offset >= 0 && offset + 4 <= fileHeader.Length)
                {
                    byte[] head2 = new byte[4];
                    Array.Copy(fileHeader, offset, head2, 0, 4);

                    if (head2[0] == 0x76 && head2[1] == 0x74 && head2[2] == 0x78 && head2[3] == 0x00)
                        return ".vtxl";

                    if (head2[0] == 0x78 && head2[1] == 0x74 && head2[2] == 0x78 && head2[3] == 0x00)
                        return ".xtxl";
                }
            }
            catch
            {
            }

            try
            {
                uint sizeBytes = BitConverter.ToUInt32(magic, 0);

                if (fileSize > 4 && sizeBytes > (fileSize - 4))
                    return ".lz77";
            }
            catch
            {
            }

            return "";
        }

        static void ExtractAll(string cpkPath, string outputDir)
        {
            if (Directory.Exists(outputDir))
            {
                Console.WriteLine($"Output directory already exists, deleting: {outputDir}");
                try
                {
                    Directory.Delete(outputDir, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting directory: {ex.Message}");
                    Console.WriteLine("Try to continue with existing directory...");
                }
            }

            cpkContentName = cpkPath;
            cpkContent.ReadCPK(cpkPath, Encoding.UTF8);

            string finalOutputDir = outputDir;
            if (!Directory.Exists(finalOutputDir))
            {
                Directory.CreateDirectory(finalOutputDir);
            }

            List<FileEntry> entries = cpkContent.fileTable.Where(x => x.FileType == "FILE").ToList();

            if (entries.Count == 0)
            {
                Console.WriteLine("Warring: Not find file in CPK");
                return;
            }

            Console.WriteLine($"Find {entries.Count} files.");

            using (BinaryReader reader = new BinaryReader(File.OpenRead(cpkPath)))
            {
                bool hasDuplicateIds = CheckDuplicateIds(entries);

                for (int i = 0; i < entries.Count; i++)
                {
                    FileEntry entry = entries[i];

                    float progress = (float)i / entries.Count * 100f;
                    Console.Write($"\rExtract Progress: {progress:F1}% ({i + 1}/{entries.Count})");

                    if (!string.IsNullOrEmpty((string)entry.DirName))
                    {
                        string dirPath = Path.Combine(finalOutputDir, (string)entry.DirName);
                        if (!Directory.Exists(dirPath))
                        {
                            Directory.CreateDirectory(dirPath);
                        }
                    }

                    string fileName;
                    int id = entry.ID == null ? -1 : Convert.ToInt32(entry.ID);

                    if (id >= 0 && hasDuplicateIds)
                    {
                        fileName = (string.IsNullOrEmpty((string)entry.DirName) ? "" : entry.DirName + "/") +
                                  $"[{id}]" + entry.FileName;
                    }
                    else
                    {
                        fileName = (string.IsNullOrEmpty((string)entry.DirName) ? "" : entry.DirName + "/") + entry.FileName;
                    }

                    fileName = fileName.TrimStart('/');

                    reader.BaseStream.Seek((long)entry.FileOffset, SeekOrigin.Begin);

                    string magic = Encoding.ASCII.GetString(reader.ReadBytes(8));
                    reader.BaseStream.Seek((long)entry.FileOffset, SeekOrigin.Begin);

                    byte[] fileData = reader.ReadBytes(Int32.Parse(entry.FileSize.ToString()));

                    if (magic == "CRILAYLA")
                    {
                        int decompressedSize = Int32.Parse((entries[i].ExtractSize ?? entries[i].FileSize).ToString());
                        if (decompressedSize != 0)
                        {
                            fileData = cpkContent.DecompressLegacyCRI(fileData, decompressedSize);
                        }
                    }

                    if (cpkContent.isNamelessPack)
                    {
                        fileName += DetectFileExtension(fileData, fileData.Length);
                    }

                    string outputPath = Path.Combine(finalOutputDir, fileName);

                    outputPath = GetUniqueFilePath(outputPath);

                    string outputDirPath = Path.GetDirectoryName(outputPath);
                    if (!Directory.Exists(outputDirPath))
                    {
                        Directory.CreateDirectory(outputDirPath);
                    }

                    File.WriteAllBytes(outputPath, fileData);
                }
            }
        }

        static string GetUniqueFilePath(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return filePath;
            }

            string directory = Path.GetDirectoryName(filePath);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);
            int counter = 1;

            string newFilePath;
            do
            {
                newFilePath = Path.Combine(directory, $"{fileNameWithoutExt}_[{counter}]{extension}");
                counter++;
            }
            while (File.Exists(newFilePath));

            return newFilePath;
        }

        static void ReplaceCommand(string[] args)
        {
            string cpkPath = "";
            string inputDir = "";
            string outputCpk = "";
            bool uncompressed = false;
            bool nameless = false;

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "-p" && i + 1 < args.Length)
                {
                    cpkPath = args[i + 1];
                    i++;
                }
                else if (args[i] == "-i" && i + 1 < args.Length)
                {
                    inputDir = args[i + 1];
                    i++;
                }
                else if (args[i] == "-o" && i + 1 < args.Length)
                {
                    outputCpk = args[i + 1];
                    i++;
                }
                else if (args[i] == "-nc")
                {
                    uncompressed = true;
                }
                else if (args[i] == "-nl")
                {
                    nameless = true;
                }
            }

            if (string.IsNullOrEmpty(cpkPath) || string.IsNullOrEmpty(inputDir) || string.IsNullOrEmpty(outputCpk))
            {
                Console.WriteLine("Error: miss args");
                PrintUsage();
                return;
            }

            if (!File.Exists(cpkPath))
            {
                Console.WriteLine($"Error: CPK file not exist => {cpkPath}");
                return;
            }

            if (!Directory.Exists(inputDir))
            {
                Console.WriteLine($"Error: output dir not exist => {inputDir}");
                return;
            }

            Console.WriteLine($"Will patch: {cpkPath}");
            Console.WriteLine($"Input dir: {inputDir}");
            Console.WriteLine($"Patch CPK: {outputCpk}");
            Console.WriteLine($"Compressed: {(uncompressed ? "No" : "Yes")}");

            ReplaceFiles(cpkPath, inputDir, outputCpk, uncompressed, nameless);

            Console.WriteLine("Patch finished!");
        }

        private static void GetFilesFromPath(string directoryname, ref List<string> ls)
        {
            FileInfo[] fi = new DirectoryInfo(directoryname).GetFiles();
            DirectoryInfo[] di = new DirectoryInfo(directoryname).GetDirectories();
            if (fi.Length != 0)
            {
                foreach (FileInfo v in fi)
                {
                    ls.Add(v.FullName);
                }
            }
            if (di.Length != 0)
            {
                foreach (DirectoryInfo v in di)
                {
                    GetFilesFromPath(v.FullName, ref ls);

                }
            }
        }

        static void ReplaceFiles(string cpkPath, string inputDir, string outputCpk, bool uncompressed, bool nameless)
        {
            cpkContentName = cpkPath;
            cpkContent.isNamelessPack = nameless;
            cpkContent.ReadCPK(cpkPath, Encoding.UTF8);

            List<string> inputFiles = GetAllFiles(inputDir);
            Console.WriteLine($"Find {inputFiles.Count} patch files.");

            Dictionary<string, string> fileMap = new Dictionary<string, string>();

            List<string> ls = new List<string>();
            GetFilesFromPath(inputDir, ref ls);

            foreach (string s in ls)
            {
                fileMap.Add("/" + Path.GetFileName(s), s);
            }

            Console.WriteLine("Start patch CPK ...");

            PatchCPK patcher = new PatchCPK(cpkContent, cpkContentName);
            patcher.SetListener(
                (float value) =>
                {
                    Console.WriteLine($"Progress ==> {value}...");
                },
                (string msg) =>
                {
                    Console.WriteLine(msg);
                },
                () => {
                    Console.WriteLine("CPK Patched.");
                }
            );
            patcher.Patch(outputCpk, uncompressed, fileMap);
        }

        static List<string> GetAllFiles(string directory)
        {
            List<string> files = new List<string>();

            try
            {
                string[] currentFiles = Directory.GetFiles(directory);
                files.AddRange(currentFiles);

                string[] subDirectories = Directory.GetDirectories(directory);
                foreach (string subDir in subDirectories)
                {
                    files.AddRange(GetAllFiles(subDir));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {directory}: {ex.Message}");
            }

            return files;
        }
    }
}
