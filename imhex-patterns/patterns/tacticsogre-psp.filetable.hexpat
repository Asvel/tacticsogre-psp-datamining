#pragma endian little

// https://github.com/gibbed/Gibbed.LetUsClingTogether/blob/main/projects/Gibbed.LetUsClingTogether.FileFormats/FileTableFile.cs

using BatchHeader;
using FileHeader;
u32 batchHeaderOffset;

struct NameHeader {
	u32 nameHash;
	u16 directoryId;
	u16 fileId;
};

struct DirectoryHeader {
	u16 id;
	padding[1];
	u8 dataBlockSize;
	u32 dataBaseOffset;
	padding[1];
	bool isInIntallData;
	u16 batchCount;
	u16 nameTableCount;
	u16 nameTableIndex;
	u32 batchTableOffset;
	u32 dataInstallBaseOffset;
	BatchHeader batches[batchCount] @ batchHeaderOffset + batchTableOffset;
};

bitfield BatchHeaderField {
	fileTableOffset : 29;
	flags : 3;
};
struct BatchHeader {
	u16 batchFileId;
	u16 fileCount;
	BatchHeaderField _ [[inline]];
	FileHeader files[fileCount] @ parent.parent.fileTableOffset + _.fileTableOffset;
};

struct FileHeader {
	if (parent._.flags == 2) {
		u16 dataBlockOffset;
		u16 dataSize;
	}
	if (parent._.flags == 3) {
		u16 dataBlockOffset;
		u32 dataSize;
	}
	if (parent._.flags == 4) {
		u32 dataBlockOffset;
		u16 dataSize;
	}
	if (parent._.flags == 5) {
		u32 dataBlockOffset;
		u32 dataSize;
	}
};

struct FileTable {
	char signature[2];
	u16 nameCount;
	u16 directoryCount;
	u16 _unknown1;
	u32 fileTableOffset;
	u32 totalSize;
	char titleId1[16];
	char titleId2[16];
	u8 _unknown2[3];
	u8 parentalLevel;
	u8 installDataCryptoKey[16];
	NameHeader names[nameCount];
	batchHeaderOffset = $ + 0x18 * directoryCount;
	DirectoryHeader directories[directoryCount];
};

FileTable filetbale @ 0x00;
