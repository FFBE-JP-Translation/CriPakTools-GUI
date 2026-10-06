using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibCPK
{
    public class PatchCPK
    {
        private CPK cpk;
        private string cpkContentName;
        private Action<float> onProgressChanged;
        private Action<string> onMsgUpdateChanged;
        private Action onCompleteChanged;

        /// <summary>
        /// Archive uses the FFBE JP format: every file is encrypted with a key derived from its file name.
        /// Patch files are encrypted before they are stored.
        /// </summary>
        public bool FfbeJpFormat { get; set; }

        /// <summary>
        /// Data alignment of the archive (CPK header "Align"), like the official CPK maker uses.
        /// </summary>
        private int GetAlign()
        {
            object value;
            if (cpk.cpkdata != null && cpk.cpkdata.TryGetValue("Align", out value) && value != null)
            {
                int align = Convert.ToInt32(value);
                if (align > 0)
                {
                    return align;
                }
            }
            return 0x800;
        }

        private static long AlignUp(long value, int align)
        {
            return (value + align - 1) / align * align;
        }

        private static void WriteZeros(BinaryWriter writer, long count)
        {
            if (count <= 0)
            {
                return;
            }
            byte[] zeros = new byte[Math.Min(count, 0x10000)];
            while (count > 0)
            {
                int n = (int)Math.Min(count, zeros.Length);
                writer.Write(zeros, 0, n);
                count -= n;
            }
        }

        /// <summary>
        /// The header totals follow the size of the stored files. EnabledPackedSize/EnabledDataSize are
        /// a multiple of the per file sum in archives made by the official tool, keep that factor.
        /// </summary>
        private void UpdateHeaderTotal(string column, long oldSum, long newSum)
        {
            object current;
            if (cpk.cpkdata == null || !cpk.cpkdata.TryGetValue(column, out current) || current == null || oldSum <= 0)
            {
                return;
            }

            long oldTotal = Convert.ToInt64(current);
            long factor = (oldTotal % oldSum == 0 && oldTotal / oldSum > 0) ? oldTotal / oldSum : 1;
            cpk.UpdateHeaderValue(column, newSum * factor);
        }
        public PatchCPK(CPK pCpk, string oldContentName)
        {
            this.cpk = pCpk;
            //MainApp.Instance.currentPackage.CpkContentName;
            this.cpkContentName = oldContentName;
        }

        public void SetListener(Action<float> onProgressChangedEvent, Action<string> onMsgUpdateEvent, Action onCompleteEvent)
        {
            this.onProgressChanged = onProgressChangedEvent;
            this.onMsgUpdateChanged = onMsgUpdateEvent;
            this.onCompleteChanged = onCompleteEvent;
        }

        private string RemoveExtensionFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            if (path.StartsWith("/"))
            {
                int lastSlash = path.LastIndexOf('/');
                int lastDot = path.LastIndexOf('.');

                if (lastDot > lastSlash)
                {
                    return path.Substring(0, lastDot);
                }
                return path;
            }

            int lastDotIndex = path.LastIndexOf('.');
            int lastSlashIndex = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));

            if (lastDotIndex > lastSlashIndex)
            {
                return path.Substring(0, lastDotIndex);
            }

            return path;
        }

        public void Patch(string outputFilePath, bool bForceCompress, Dictionary<string, string> batch_file_list)
        {
            Dictionary<string, string> fileMap = batch_file_list;

            // Build nameless map
            if (cpk.isNamelessPack)
            {
                fileMap = new Dictionary<string, string>();
                foreach (var kvp in batch_file_list)
                {
                    string key = kvp.Key;
                    string value = kvp.Value;

                    string keyWithoutExt = RemoveExtensionFromPath(key);
                    fileMap[keyWithoutExt] = value;

                    Debug.Print($"Nameless map: '{key}' -> '{keyWithoutExt}' -> '{value}'");
                }
            }

            string msg;
            BinaryReader oldFile = new BinaryReader(File.OpenRead(this.cpkContentName));
            string outputName = outputFilePath;

            BinaryWriter newCPK = new BinaryWriter(new FileStream(outputName, FileMode.Create, FileAccess.Write));

            cpk.FfbeJpFormat = FfbeJpFormat;
            int align = GetAlign();
            long contentEnd = 0;
            long oldPackedSum = 0, oldDataSum = 0, newPackedSum = 0, newDataSum = 0;

            List<FileEntry> entries = cpk.fileTable.OrderBy(x => x.FileOffset).ToList();

            int id;
            bool bFileRepeated = Tools.CheckListRedundant(entries);
            for (int i = 0; i < entries.Count; i++)
            {
                onProgressChanged?.Invoke((float)i / (float)entries.Count * 100f);
                if (entries[i].FileType != "CONTENT")
                {
                    id = Convert.ToInt32(entries[i].ID);
                    string currentName;

                    if (id > 0 && bFileRepeated)
                    {
                        currentName = (((entries[i].DirName != null) ?
                                        entries[i].DirName + "/" : "") + string.Format("[{0}]", id.ToString()) + entries[i].FileName);
                    }
                    else
                    {
                        currentName = ((entries[i].DirName != null) ? entries[i].DirName + "/" : "") + entries[i].FileName;
                    }
                    if (!currentName.Contains("/"))
                    {
                        currentName = "/" + currentName;
                    }
                    if (entries[i].FileType == "FILE")
                    {
                        // I'm too lazy to figure out how to update the ContextOffset position so this works :)
                        if ((ulong)newCPK.BaseStream.Position < cpk.ContentOffset)
                        {
                            ulong padLength = cpk.ContentOffset - (ulong)newCPK.BaseStream.Position;
                            for (ulong z = 0; z < padLength; z++)
                            {
                                newCPK.Write((byte)0);
                            }
                        }

                        
                        Debug.Print("Got File:" + currentName.ToString());

                        if (!fileMap.Keys.Contains(currentName.ToString()))
                        //如果不在表中，复制原始数据
                        {
                            oldFile.BaseStream.Seek((long)entries[i].FileOffset, SeekOrigin.Begin);
                            entries[i].FileOffset = (ulong)newCPK.BaseStream.Position;
                            if (entries[i].FileName.ToString() == "ETOC_HDR")
                            {

                                cpk.EtocOffset = entries[i].FileOffset;
                                onMsgUpdateChanged?.Invoke(string.Format("Fix ETOC_OFFSET to {0:x8}", cpk.EtocOffset));
                            }
                            onMsgUpdateChanged?.Invoke(string.Format("Update Entry: {0}, {1:x8}", entries[i].FileName, entries[i].FileOffset));
                            cpk.UpdateFileEntry(entries[i]);

                            byte[] chunk = oldFile.ReadBytes(Int32.Parse(entries[i].FileSize.ToString()));
                            newCPK.Write(chunk);
                            oldPackedSum += chunk.Length;
                            newPackedSum += chunk.Length;
                            oldDataSum += Convert.ToInt64(entries[i].ExtractSize ?? entries[i].FileSize);
                            newDataSum += Convert.ToInt64(entries[i].ExtractSize ?? entries[i].FileSize);

                            contentEnd = newCPK.BaseStream.Position;
                            if (i < entries.Count - 1)
                            {
                                WriteZeros(newCPK, AlignUp(contentEnd, align) - contentEnd);
                            }

                        }
                        else
                        {
                            string replace_with = fileMap[currentName.ToString()];
                            //Got patch file name

                            onMsgUpdateChanged?.Invoke(string.Format("Patching: {0}", currentName.ToString()));

                            byte[] newbie = File.ReadAllBytes(replace_with);
                            entries[i].FileOffset = (ulong)newCPK.BaseStream.Position;
                            int o_ext_size = Int32.Parse((entries[i].ExtractSize).ToString());
                            int o_com_size = Int32.Parse((entries[i].FileSize).ToString());
                            byte[] stored = newbie;
                            if ((o_com_size < o_ext_size) && entries[i].FileType == "FILE" && bForceCompress == true)
                            {
                                // is compressed
                                msg = string.Format("Compressing data:{0:x8}", newbie.Length);
                                onMsgUpdateChanged?.Invoke(msg);

                                stored = cpk.CompressCRILAYLA(newbie);
                            }
                            else
                            {
                                msg = string.Format("Storing data:{0:x8}\r\n", newbie.Length);
                                onMsgUpdateChanged?.Invoke(msg);
                            }

                            if (FfbeJpFormat)
                            {
                                // stored data = compressed (optional) data, encrypted with the file name key
                                stored = AssetCipher.Encrypt(stored, entries[i].FileName.ToString());
                            }

                            oldPackedSum += o_com_size;
                            oldDataSum += o_ext_size;
                            newPackedSum += stored.Length;
                            newDataSum += newbie.Length;

                            entries[i].FileSize = Convert.ChangeType(stored.Length, entries[i].FileSizeType);
                            entries[i].ExtractSize = Convert.ChangeType(newbie.Length, entries[i].FileSizeType);
                            cpk.UpdateFileEntry(entries[i]);
                            newCPK.Write(stored);
                            onMsgUpdateChanged?.Invoke(string.Format("Update Entry: {0}, {1:x8}", entries[i].FileName, entries[i].FileOffset));
                            onMsgUpdateChanged?.Invoke(string.Format(">> {0:x8}\r\n", stored.Length));

                            contentEnd = newCPK.BaseStream.Position;
                            if (i < entries.Count - 1)
                            {
                                WriteZeros(newCPK, AlignUp(contentEnd, align) - contentEnd);
                            }
                        }
                    }
                    else
                    {
                        //Update HDR:
                        Debug.Print("Got HDR:" + currentName.ToString());
                        long oldHdrOffset = (long)entries[i].FileOffset;
                        oldFile.BaseStream.Seek(oldHdrOffset, SeekOrigin.Begin);

                        // Header packets are not resized by a patch. Keep the ones in front of the file data where
                        // they are (like the official tool lays them out), anything behind it follows the data.
                        long hdrOffset = newCPK.BaseStream.Position;
                        if (oldHdrOffset >= hdrOffset && (ulong)oldHdrOffset < cpk.ContentOffset)
                        {
                            hdrOffset = oldHdrOffset;
                        }
                        else if (entries[i].FileName.ToString() != "CPK_HDR")
                        {
                            hdrOffset = AlignUp(hdrOffset, align);
                        }
                        WriteZeros(newCPK, hdrOffset - newCPK.BaseStream.Position);

                        entries[i].FileOffset = (ulong)newCPK.BaseStream.Position;
                        if (entries[i].FileName.ToString() == "TOC_HDR")
                        {
                            cpk.TocOffset = entries[i].FileOffset;
                            onMsgUpdateChanged?.Invoke(string.Format("Fix TOC_OFFSET to {0:x8}", cpk.TocOffset));
                        }
                        if (entries[i].FileName.ToString() == "ETOC_HDR")
                        {
                            cpk.EtocOffset = entries[i].FileOffset;
                            onMsgUpdateChanged?.Invoke(string.Format("Fix ETOC_OFFSET to {0:x8}", cpk.EtocOffset));
                        }
                        if (entries[i].FileName.ToString() == "ITOC_HDR")
                        {
                            cpk.ItocOffset = entries[i].FileOffset;
                            onMsgUpdateChanged?.Invoke(string.Format("Fix ITOC_OFFSET to {0:x8}", cpk.ItocOffset));
                        }
                        if (entries[i].FileName.ToString() == "GTOC_HDR")
                        {
                            cpk.GtocOffset = entries[i].FileOffset;
                            onMsgUpdateChanged?.Invoke(string.Format("Fix GTOC_OFFSET to {0:x8}", cpk.GtocOffset));
                        }
                        onMsgUpdateChanged?.Invoke(string.Format("Update HDR Entry: {0}, {1:x8}", entries[i].FileName, entries[i].FileOffset));
                        cpk.UpdateFileEntry(entries[i]);

                        // The packet (fourcc + marker + size + utf table) is rewritten at the end, reserve its space.
                        byte[] chunk = oldFile.ReadBytes(Int32.Parse(entries[i].FileSize.ToString()) + 0x10);
                        newCPK.Write(chunk);
                    }
                }
                else
                {
                    // Content is special.... just update the position
                    onMsgUpdateChanged?.Invoke(string.Format("Update Special Entry: {0}, {1:x8}", entries[i].FileName, entries[i].FileOffset));
                    cpk.UpdateFileEntry(entries[i]);
                }
            }

            if (contentEnd > (long)cpk.ContentOffset)
            {
                cpk.UpdateHeaderValue("ContentSize", AlignUp(contentEnd - (long)cpk.ContentOffset, align));
            }
            UpdateHeaderTotal("EnabledPackedSize", oldPackedSum, newPackedSum);
            UpdateHeaderTotal("EnabledDataSize", oldDataSum, newDataSum);

            cpk.WriteCPK(newCPK);
            msg = string.Format("Writing TOC....");

            onMsgUpdateChanged?.Invoke(msg);

            cpk.WriteITOC(newCPK);
            cpk.WriteTOC(newCPK);
            cpk.WriteETOC(newCPK);
            cpk.WriteGTOC(newCPK);

            newCPK.Close();
            oldFile.Close();
            msg = string.Format("Saving CPK to {0}....", outputName);
            onMsgUpdateChanged?.Invoke(msg);
            Debug.Print(msg);
            onCompleteChanged.Invoke();
        }
    }
}
