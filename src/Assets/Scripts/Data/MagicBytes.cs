using System;
using System.IO;
using System.Linq;

namespace PatchKit.Unity.Patcher.Data
{

public class MagicBytes
{
    #region Fields

    public static FileType MachO32 = new FileType(new byte[] {0xFE, 0xED, 0xFA, 0xCE}, 0x1000);
    public static FileType MachO64 = new FileType(new byte[] {0xFE, 0xED, 0xFA, 0xCF}, 0x1000);
    public static FileType MachO32Reverse = new FileType(new byte[] {0xCE, 0xFA, 0xED, 0xFE}, 0);
    public static FileType MachO64Reverse = new FileType(new byte[] {0xCF, 0xFA, 0xED, 0xFE}, 0);

    // ReSharper disable once InconsistentNaming
    public static FileType ELF = new FileType(new byte[] {0x7F, 0x45, 0x4C, 0x46}, 0);

    public static FileType[] AllKnown =
    {
        MachO32,
        MachO64,
        MachO32Reverse,
        MachO64Reverse,
        ELF
    };

    public static FileType[] MacExecutables =
    {
        MachO32,
        MachO64,
        MachO32Reverse,
        MachO64Reverse,
    };

    public static FileType[] LinuxExecutables =
    {
        ELF
    };

    #endregion

    #region Methods

    public static bool IsMacExecutable(string filename)
    {
        FileType fileType = ReadFileType(filename);
        return IsWithin(MacExecutables, fileType);
    }

    public static bool IsLinuxExecutable(string filename)
    {
        FileType fileType = ReadFileType(filename);
        return IsWithin(LinuxExecutables, fileType);
    }

    private static bool IsWithin(FileType[] types, FileType needle)
    {
        if (needle == null)
        {
            return false;
        }

        return Array.IndexOf(types, needle) != -1;
    }

    public static FileType ReadFileType(string filename)
    {
        using (var reader = new BinaryReader(new FileStream(filename, FileMode.Open)))
        {
            foreach (var fileType in AllKnown)
            {
                if (fileType.Matches(reader))
                {
                    return fileType;
                }
            }
        }

        return null;
    }

    #endregion

    #region Inner Types

    public class FileType
    {
        public byte[] MagicBytes { get; private set; }
        public int Offset { get; private set; }

        public FileType(byte[] magicBytes, int offset)
        {
            MagicBytes = magicBytes;
            Offset = offset;
        }

        public bool Matches(BinaryReader reader)
        {
            var buffer = new byte[MagicBytes.Length];

            reader.BaseStream.Seek(Offset, SeekOrigin.Begin);
            reader.Read(buffer, 0, buffer.Length);

            return MagicBytes.SequenceEqual(buffer);
        }
    }

    #endregion
}

} // namespace
