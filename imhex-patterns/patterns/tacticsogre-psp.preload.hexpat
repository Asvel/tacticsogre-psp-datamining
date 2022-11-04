#pragma endian little

struct PreloadIndex {
	u16 directoryId;
	u16 fileId;
	u32 size;
};

struct Preload {
	u32 count;
	PreloadIndex indexes[count];
};

Preload preload @ 0x00 [[inline]];
