using Gibbed.LetUsClingTogether.FileFormats;
using Gibbed.LetUsClingTogether.FileFormats.FileTable;

namespace TacticsogrePspExtracts
{
    public class GameDataLoader
    {
        readonly string basePath;
        readonly FileTableFile table = new();
        readonly Dictionary<(uint, uint), byte[]> preload = new();

        FileStream? diskStream;
        BinaryReader? reader;
        uint fileOffset;
        uint[]? childOffsets;

        public DirectoryEntry? DirectoryEntry { get; private set; }
        public FileEntry? FileEntry { get; private set; }
        public uint[]? ChildIds { get; private set; }

        public void PrepareDirectory(uint directoryId)
        {
            if (DirectoryEntry?.Id == directoryId) return;
            
            DirectoryEntry = table.Directories.Find(d => d.Id == directoryId);
            if (DirectoryEntry is null) throw new FileNotFoundException();

            diskStream?.Close();
            diskStream = OpenRead(directoryId.ToString("X4"));

            reader = null;
            FileEntry = null;            
        }
        public void PrepareFile(uint fileId)
        {
            if (FileEntry?.Id == fileId) return;

            FileEntry = DirectoryEntry?.Files.Find(d => d.Id == fileId);
            if (FileEntry?.Id != fileId) FileEntry = null;
            if (FileEntry is null) throw new FileNotFoundException();

            PrepareFile(FileEntry.Value);
        }
        public void PrepareFile(FileEntry fileEntry)
        {
            if (DirectoryEntry == null) throw new InvalidOperationException();
            FileEntry = fileEntry;
            childOffsets = null;
            if (preload.TryGetValue((DirectoryEntry.Id, fileEntry.Id), out var data))
            {
                fileOffset = 0;
                reader = new BinaryReader(new MemoryStream(data));
            }
            else
            {
                fileOffset = DirectoryEntry.DataBaseOffset +
                    (fileEntry.DataBlockOffset << DirectoryEntry.DataBlockSize) *
                    FileTableFile.BaseDataBlockSize;
                diskStream!.Position = fileOffset;
                reader = new BinaryReader(diskStream);
            }
        }
        public void PreparePakd()
        {
            if (childOffsets != null) return;
            if (reader?.ReadUInt32() != 0x646B6170/*pakd*/) throw new InvalidDataException();

            var childCount = reader.ReadUInt32();
            childOffsets = new uint[childCount + 1];
            for (var i = 0u; i < childCount + 1; i++) childOffsets[i] = reader.ReadUInt32();
            if (fileOffset + childOffsets[0] >= reader.BaseStream.Position + childCount * 4)
            {
                ChildIds = new uint[childCount];
                for (var i = 0u; i < childCount; i++) ChildIds[i] = reader.ReadUInt32();
            }
            else
            {
                ChildIds = null;
            }
        }
        public byte[] ReadChild(int childIndex, uint pickSize = uint.MaxValue)
        {
            if (reader == null || childOffsets == null) throw new InvalidOperationException();
            reader.BaseStream.Position = fileOffset + childOffsets[childIndex];
            var childSize = childOffsets[childIndex + 1] - childOffsets[childIndex];
            return reader.ReadBytes((int)Math.Min(childSize, pickSize));
        }

        public byte[]? Load(uint directoryId, uint fileId, uint childId)
        {
            PrepareDirectory(directoryId);
            PrepareFile(fileId);
            PreparePakd();

            var childIndex = ChildIds != null ? Array.IndexOf(ChildIds, childId) : (int)childId;
            if (childIndex == -1) return null;
            return ReadChild(childIndex);
        }
        public byte[]? Load(uint directoryId, uint fileId)
        {
            PrepareDirectory(directoryId);
            PrepareFile(fileId);
            diskStream!.Position = fileOffset;
            return reader!.ReadBytes((int)FileEntry!.Value.DataSize);
        }

        FileStream OpenRead(string name)
        {
            return File.Open(Path.Combine(basePath, $"{name}.BIN"),
                FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public GameDataLoader(string basePath)
        {
            this.basePath = basePath;

            using var tableFile = OpenRead("FILETABLE");
            table.Deserialize(tableFile);

            PrepareDirectory(0);
            PrepareFile(0);
            var count = reader!.ReadInt32();
            var header = new BinaryReader(new MemoryStream(reader.ReadBytes(count * 8)));
            for (var i = 0u; i < count; i++)
            {
                var directoryId = header.ReadUInt16();
                var fileId = header.ReadUInt16();
                var size = header.ReadInt32();
                preload.Add((directoryId, fileId), reader.ReadBytes(size));
            }
        }
        ~GameDataLoader()
        {
            diskStream?.Close();
        }
    }
}
