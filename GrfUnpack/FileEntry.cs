namespace GrfUnpack
{
	public class FileEntry
	{
		public string FileName { get; set; }
		public EntryType Type { get; set; }
		public int Offset { get; set; }
		public int SizeCompressed { get; set; }
		public int SizeOriginal { get; set; }
	}

	public enum EntryType : byte
	{
		UncompressedFile = 0,
		CompressedFile = 1,
		Folder = 2,
	}
}
