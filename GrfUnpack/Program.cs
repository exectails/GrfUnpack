using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace GrfUnpack
{
	internal class Program
	{
		static Encoding FileNameEncoding = Encoding.GetEncoding("Windows-1252");

		static void Main(string[] args)
		{
			var filePath = "";
			var outFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "output");

			if (args.Length == 0)
			{
				Console.WriteLine("Usage: {0} [options] <file> [output folder]", Path.GetFileName(Assembly.GetCallingAssembly().Location));
				Console.WriteLine("");
				Console.WriteLine("Options:");
				Console.WriteLine("  -k    Save files with Korean names.");
				return;
			}

			var set = 0;
			foreach (var arg in args)
			{
				if (arg.StartsWith("-k"))
				{
					FileNameEncoding = Encoding.GetEncoding("EUC-KR");
				}
				else if (set == 0)
				{
					filePath = arg;
					set++;
				}
				else if (set == 1)
				{
					outFolderPath = arg;
					set++;
				}
			}

			if (!File.Exists(filePath))
			{
				Console.WriteLine("File '{0}' not found.", filePath);
				return;
			}

			ExtractArchive(filePath, outFolderPath);
		}

		private static void ExtractArchive(string filePath, string outFolderPath)
		{
			using (var fs = new FileStream(filePath, FileMode.Open))
			using (var br = new BinaryReader(fs))
			{
				List<FileEntry> entries = null;

				var signature = Encoding.ASCII.GetString(br.ReadBytes(5));

				// PAK File (Arcturus, Version "0")
				if (signature == "\0GRSC")
				{
					try
					{
						entries = ReadEntries(br, version: 0);
					}
					catch (Exception ex)
					{
						Console.WriteLine("An error occurred while reading the PAK archive: {0}", ex);
						return;
					}
				}
				// GRF File (RO, Version "1")
				// The only differences are that GRF uses encoded file names
				// and a slightly different footer.
				else if (signature == "\0GRAT")
				{
					try
					{
						entries = ReadEntries(br, version: 1);
					}
					catch (Exception ex)
					{
						Console.WriteLine("An error occurred while reading the GRF archive: {0}", ex);
						return;
					}
				}
				else
				{
					Console.WriteLine("Invalid file format.");
					return;
				}

				if (entries.Count == 0)
				{
					Console.WriteLine("No files found in archive.");
					return;
				}

				ExtractFiles(entries, outFolderPath, br, ReportProgress);
			}
		}

		private static void ReportProgress(int count, int countTotal, string fileName)
		{
			Console.WriteLine("{0}/{1} : {2}", count, countTotal, fileName);
		}

		private static void ExtractFiles(List<FileEntry> entries, string outFolderPath, BinaryReader br, Action<int, int, string> progressCallback)
		{
			for (var i = 0; i < entries.Count; i++)
			{
				var entry = entries[i];
				var outFilePath = Path.Combine(outFolderPath, entry.FileName);

				progressCallback(i + 1, entries.Count, entry.FileName);

				if (entry.Type == EntryType.Folder)
				{
					if (!Directory.Exists(outFilePath))
						Directory.CreateDirectory(outFilePath);
				}
				else if (entry.Type == EntryType.File)
				{
					var outFileParentPath = Path.GetDirectoryName(outFilePath);

					if (!Directory.Exists(outFileParentPath))
						Directory.CreateDirectory(outFileParentPath);

					br.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);

					var compressed = br.ReadBytes(entry.SizeCompressed);
					var uncompressed = UncompressData((ulong)entry.SizeCompressed, (ulong)entry.SizeOriginal, compressed);

					File.WriteAllBytes(outFilePath, uncompressed);
				}
				else
				{
					throw new InvalidDataException($"Unknown entry type '{entry.Type}'");
				}
			}
		}

		private static List<FileEntry> ReadEntries(BinaryReader br, int version)
		{
			var result = new List<FileEntry>();

			br.BaseStream.Seek(-9, SeekOrigin.End);

			int entryTableOffset, entryCount;
			if (version == 0)
			{
				entryTableOffset = br.ReadInt32();
				entryCount = br.ReadInt32();
			}
			else // Version 1+
			{
				entryTableOffset = br.ReadInt32();
				_ = br.ReadInt16();
				entryCount = br.ReadInt16();
				_ = br.ReadByte();
			}

			br.BaseStream.Seek(entryTableOffset, SeekOrigin.Begin);
			for (var i = 0; i < entryCount; ++i)
			{
				var strLen = br.ReadByte();

				var entry = new FileEntry();
				entry.Type = (EntryType)br.ReadByte();
				entry.Offset = br.ReadInt32();
				entry.SizeCompressed = br.ReadInt32();
				entry.SizeOriginal = br.ReadInt32();

				byte[] nameBytes;
				if (version > 0)
				{
					nameBytes = new byte[strLen + 1];
					for (var j = 0; j < strLen + 1; ++j)
						nameBytes[j] = DecodeString(br.ReadByte());
				}
				else
				{
					nameBytes = br.ReadBytes(strLen + 1);
				}

				entry.FileName = FileNameEncoding.GetString(nameBytes).TrimEnd('\0');

				result.Add(entry);
			}

			return result;
		}

		static byte DecodeString(byte a1)
		{
			var result = (byte)(16 * a1 ^ (a1 >> 4) & 0xF);

			if (result == 47)
				result = 92;

			return result;
		}

		static byte[] UncompressData(ulong sizeCompressed, ulong sizeOriginal, byte[] content)
		{
			var result = new byte[sizeOriginal];
			ulong resultIndex = 0, bytesRead = 0;

			while (bytesRead < sizeCompressed)
			{
				var mask = content[bytesRead++];

				for (var i = 0; i < 8 && bytesRead < sizeCompressed; i++)
				{
					if ((mask & 1) != 0)
					{
						var m0 = content[bytesRead++];
						var m1 = content[bytesRead++];

						var deslocamento = (byte)((m1 >> 4) + 2);
						var index_offset = (ushort)(((m1 & 0xF) << 8) + m0);

						Buffer.BlockCopy(result, (int)(resultIndex - index_offset), result, (int)resultIndex, deslocamento);

						resultIndex += deslocamento;
					}
					else
					{
						result[resultIndex++] = content[bytesRead++];
					}

					mask >>= 1;
				}
			}

			return result;
		}
	}
}
