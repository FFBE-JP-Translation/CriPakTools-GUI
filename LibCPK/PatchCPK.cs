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
        /// Patch an archive in the "new" format: replacement files are scrambled with their
        /// per-file key and the original header/TOC area (including its padding) is kept verbatim.
        /// </summary>
        public bool NewFormat { get; set; }
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
            if (NewFormat)
            {
                PatchNewFormat(outputFilePath, bForceCompress, batch_file_list);
                return;
            }

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

            BinaryWriter newCPK = new BinaryWriter(File.OpenWrite(outputName));

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

                            if ((newCPK.BaseStream.Position % 0x800) > 0 && i < entries.Count - 1)
                            {
                                long cur_pos = newCPK.BaseStream.Position;
                                for (int j = 0; j < (0x800 - (cur_pos % 0x800)); j++)
                                {
                                    newCPK.Write((byte)0);
                                }
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
                            if ((o_com_size < o_ext_size) && entries[i].FileType == "FILE" && bForceCompress == true)
                            {
                                // is compressed
                                msg = string.Format("Compressing data:{0:x8}", newbie.Length);
                                onMsgUpdateChanged?.Invoke(msg);

                                byte[] dest_comp = cpk.CompressCRILAYLA(newbie);

                                entries[i].FileSize = Convert.ChangeType(dest_comp.Length, entries[i].FileSizeType);
                                entries[i].ExtractSize = Convert.ChangeType(newbie.Length, entries[i].FileSizeType);
                                cpk.UpdateFileEntry(entries[i]);
                                newCPK.Write(dest_comp);
                                onMsgUpdateChanged?.Invoke(string.Format("Update Entry: {0}, {1:x8}", entries[i].FileName, entries[i].FileOffset));
                                onMsgUpdateChanged?.Invoke(string.Format(">> {0:x8}\r\n", dest_comp.Length));
                            }

                            else
                            {
                                msg = string.Format("Storing data:{0:x8}\r\n", newbie.Length);
                                onMsgUpdateChanged?.Invoke(msg);
                                entries[i].FileSize = Convert.ChangeType(newbie.Length, entries[i].FileSizeType);
                                entries[i].ExtractSize = Convert.ChangeType(newbie.Length, entries[i].FileSizeType);
                                cpk.UpdateFileEntry(entries[i]);
                                newCPK.Write(newbie);
                                onMsgUpdateChanged?.Invoke(string.Format("Update Entry: {0}, {1:x8}", entries[i].FileName, entries[i].FileOffset));
                            }


                            if ((newCPK.BaseStream.Position % 0x800) > 0 && i < entries.Count - 1)
                            {
                                long cur_pos = newCPK.BaseStream.Position;
                                for (int j = 0; j < (0x800 - (cur_pos % 0x800)); j++)
                                {
                                    newCPK.Write((byte)0);
                                }
                            }
                        }
                    }
                    else
                    {
                        //Update HDR:
                        Debug.Print("Got HDR:" + currentName.ToString());
                        oldFile.BaseStream.Seek((long)entries[i].FileOffset, SeekOrigin.Begin);
                        entries[i].FileOffset = (ulong)newCPK.BaseStream.Position;
                        if (entries[i].FileName.ToString() == "CPK_HDR")
                        {

                        }
                        if (entries[i].FileName.ToString() == "TOC_HDR")
                        {
                            cpk.EtocOffset = entries[i].FileOffset;
                            onMsgUpdateChanged?.Invoke(string.Format("Fix ETOC_OFFSET to {0:x8}", cpk.EtocOffset));
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
                            onMsgUpdateChanged?.Invoke(string.Format("Fix ITOC_OFFSET to {0:x8}", cpk.GtocOffset));
                        }
                        onMsgUpdateChanged?.Invoke(string.Format("Update HDR Entry: {0}, {1:x8}", entries[i].FileName, entries[i].FileOffset));
                        cpk.UpdateFileEntry(entries[i]);

                        byte[] chunk = oldFile.ReadBytes(Int32.Parse(entries[i].FileSize.ToString()));
                        newCPK.Write(chunk);

                        if ((newCPK.BaseStream.Position % 0x800) > 0 && i < entries.Count - 1)
                        {
                            long cur_pos = newCPK.BaseStream.Position;
                            for (int j = 0; j < (0x800 - (cur_pos % 0x800)); j++)
                            {
                                newCPK.Write((byte)0);
                            }
                        }
                        if (entries[i].FileName.ToString() == "TOC_HDR")
                        {
                            //IF TOC ,WRITE MORE 0x800
                            for (int j = 0; j < 0x800; j++)
                            {
                                newCPK.Write((byte)0);
                            }
                        }
                    }
                }
                else
                {
                    // Content is special.... just update the position
                    onMsgUpdateChanged?.Invoke(string.Format("Update Special Entry: {0}, {1:x8}", entries[i].FileName, entries[i].FileOffset));
                    cpk.UpdateFileEntry(entries[i]);
                }
            }

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

        private static long AlignUp(long value, int align)
        {
            return align > 1 ? (value + align - 1) / align * align : value;
        }

        private static void CopyRange(Stream source, Stream target, long offset, long length)
        {
            byte[] buffer = new byte[81920];
            source.Seek(offset, SeekOrigin.Begin);
            while (length > 0)
            {
                int read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, length));
                if (read <= 0)
                {
                    throw new EndOfStreamException("Unexpected end of CPK while copying data.");
                }
                target.Write(buffer, 0, read);
                length -= read;
            }
        }

        /// <summary>
        /// New format patching. The archive layout (header, TOC, ITOC, GTOC and their padding)
        /// is taken from the original CPK, files are re-packed back to back using the archive's
        /// alignment, and replacement files are encrypted with their per-file key.
        /// </summary>
        private void PatchNewFormat(string outputFilePath, bool bForceCompress, Dictionary<string, string> fileMap)
        {
            if (cpk.isNamelessPack)
            {
                throw new NotSupportedException("The new format can not be combined with nameless packs.");
            }

            cpk.NewFormat = true;

            List<FileEntry> entries = cpk.fileTable.Where(x => x.FileType == "FILE").OrderBy(x => x.FileOffset).ToList();
            long contentOffset = (long)cpk.ContentOffset;

            foreach (ulong tocPos in new ulong[] { cpk.TocOffset, cpk.ItocOffset, cpk.GtocOffset, cpk.EtocOffset })
            {
                if (tocPos != ulong.MaxValue && (long)tocPos >= contentOffset)
                {
                    throw new NotSupportedException("The new format expects every TOC packet to be located before the file data.");
                }
            }

            int align = 1;
            object alignValue;
            if (cpk.cpkdata != null && cpk.cpkdata.TryGetValue("Align", out alignValue) && alignValue != null)
            {
                align = Math.Max(1, Convert.ToInt32(alignValue));
            }

            bool bFileRepeated = Tools.CheckListRedundant(entries);

            string msg;
            using (FileStream oldFile = File.OpenRead(cpkContentName))
            using (FileStream newCPK = new FileStream(outputFilePath, FileMode.Create, FileAccess.ReadWrite))
            {
                // The padding between files is not zero in these archives, take it over from the original.
                byte[] padPattern = new byte[0];
                for (int i = 0; i + 1 < entries.Count; i++)
                {
                    long gapStart = (long)entries[i].FileOffset + Convert.ToInt64(entries[i].FileSize);
                    long gap = (long)entries[i + 1].FileOffset - gapStart;
                    if (gap > padPattern.Length && gap < align)
                    {
                        padPattern = new byte[gap];
                        oldFile.Seek(gapStart, SeekOrigin.Begin);
                        oldFile.Read(padPattern, 0, (int)gap);
                    }
                }

                // Header, TOC, ITOC, GTOC ... exactly as in the original archive.
                CopyRange(oldFile, newCPK, 0, contentOffset);

                long oldPackedSum = 0, oldDataSum = 0, newPackedSum = 0, newDataSum = 0;

                for (int i = 0; i < entries.Count; i++)
                {
                    onProgressChanged?.Invoke((float)i / (float)entries.Count * 100f);

                    FileEntry entry = entries[i];
                    string name = entry.FileName.ToString();
                    string currentName = (entry.DirName != null ? entry.DirName + "/" : "") + name;
                    if (entry.ID != null && Convert.ToInt32(entry.ID) > 0 && bFileRepeated)
                    {
                        currentName = (entry.DirName != null ? entry.DirName + "/" : "") +
                                      string.Format("[{0}]", Convert.ToInt32(entry.ID)) + name;
                    }
                    if (!currentName.Contains("/"))
                    {
                        currentName = "/" + currentName;
                    }

                    long oldSize = Convert.ToInt64(entry.FileSize);
                    long oldExtractSize = entry.ExtractSize != null ? Convert.ToInt64(entry.ExtractSize) : oldSize;
                    oldPackedSum += oldSize;
                    oldDataSum += oldExtractSize;

                    byte[] data;
                    int extractSize = (int)oldExtractSize;
                    bool replaced = fileMap.ContainsKey(currentName);
                    if (replaced)
                    {
                        onMsgUpdateChanged?.Invoke(string.Format("Patching: {0}", currentName));
                        byte[] newbie = File.ReadAllBytes(fileMap[currentName]);
                        extractSize = newbie.Length;
                        if (oldSize < oldExtractSize && bForceCompress)
                        {
                            onMsgUpdateChanged?.Invoke(string.Format("Compressing data:{0:x8}", newbie.Length));
                            newbie = cpk.CompressCRILAYLA(newbie);
                        }
                        data = AssetCipher.Encrypt(newbie, name);
                    }
                    else
                    {
                        // Untouched files are copied in their stored (already encrypted) form.
                        data = new byte[oldSize];
                        oldFile.Seek((long)entry.FileOffset, SeekOrigin.Begin);
                        int read = oldFile.Read(data, 0, data.Length);
                        if (read != data.Length)
                        {
                            throw new EndOfStreamException("Unexpected end of CPK while reading " + name);
                        }
                    }

                    // Align the start of the file (relative to the content area).
                    long relative = newCPK.Position - contentOffset;
                    long pad = AlignUp(relative, align) - relative;
                    for (long j = 0; j < pad; j++)
                    {
                        newCPK.WriteByte(j < padPattern.Length ? padPattern[j] : (byte)0);
                    }

                    entry.FileOffset = (ulong)newCPK.Position;
                    if (replaced)
                    {
                        entry.FileSize = Convert.ChangeType(data.Length, entry.FileSizeType);
                        entry.ExtractSize = Convert.ChangeType(extractSize, entry.ExtractSizeType);
                    }
                    cpk.UpdateFileEntry(entry);
                    newCPK.Write(data, 0, data.Length);

                    newPackedSum += data.Length;
                    newDataSum += extractSize;
                    onMsgUpdateChanged?.Invoke(string.Format("Update Entry: {0}, {1:x8}", entry.FileName, entry.FileOffset));
                }

                // CPK header: keep the totals consistent with the new file sizes.
                long contentSize = AlignUp(newCPK.Position - contentOffset, align);
                cpk.UpdateHeaderValue("ContentSize", contentSize);
                UpdateHeaderTotal("EnabledPackedSize", oldPackedSum, newPackedSum);
                UpdateHeaderTotal("EnabledDataSize", oldDataSum, newDataSum);

                onMsgUpdateChanged?.Invoke("Writing TOC....");
                BinaryWriter packetWriter = new BinaryWriter(newCPK);
                cpk.WritePacket(packetWriter, "CPK ", 0, cpk.CPK_packet);
                cpk.WriteITOC(packetWriter);
                cpk.WriteTOC(packetWriter);
                cpk.WriteETOC(packetWriter);
                cpk.WriteGTOC(packetWriter);
                packetWriter.Flush();
            }

            msg = string.Format("Saving CPK to {0}....", outputFilePath);
            onMsgUpdateChanged?.Invoke(msg);
            Debug.Print(msg);
            onCompleteChanged?.Invoke();
        }

        /// <summary>
        /// Header totals (EnabledPackedSize / EnabledDataSize) are a multiple of the per-file sums
        /// (the original tool counts every file once per file table), keep that factor.
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
    }
}
