using System;
using System.IO;
using System.Linq;
using FirmwareProviderAPI.Utils;

namespace FirmwareProviderAPI;

public class FirmwareBinary
{
    public const long FotaBinMagic = 0xCAFECAFE;
    public const long FotaBinMagicCombination = 0x42434F4D;

    public long Magic { get; }
    public long SegmentsCount { get; }
    public long TotalSize { get; }
    public int Crc32 { get; }
    public FirmwareSegment[] Segments { get; }

    public FirmwareBinary(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream); 
        
        try
        {
            Magic = reader.ReadUInt32();
            
            if (Magic != FotaBinMagic)
            {
                throw new FirmwareParseException(FirmwareParseException.ErrorCodes.InvalidMagic, "invalid magic");
            }

            TotalSize = reader.ReadInt32();
            if (TotalSize == 0)
            {
                throw new FirmwareParseException(FirmwareParseException.ErrorCodes.SizeZero, "empty container");
            }
            
            SegmentsCount = reader.ReadInt32();
            if (SegmentsCount == 0)
            {
                throw new FirmwareParseException(FirmwareParseException.ErrorCodes.NoSegmentsFound, "no segments");
            }
            
            Segments = new FirmwareSegment[SegmentsCount];
            for (var i = 0; i < SegmentsCount; i++)
            {
                Segments[i] = new FirmwareSegment(reader);
            }

            reader.BaseStream.Seek(-4, SeekOrigin.End);
            Crc32 = reader.ReadInt32();
        }
        catch (Exception ex) when (ex is not FirmwareParseException)
        {
            throw new FirmwareParseException(FirmwareParseException.ErrorCodes.Unknown, ex.ToString());
        }
    }

    public byte[] SerializeTable()
    {
        using var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
            
        writer.Write(Crc32);
        writer.Write((byte) SegmentsCount);

        foreach (var segment in Segments)
        {
            writer.Write((byte) segment.Id);
            writer.Write((int) segment.Size);
            writer.Write((int) segment.Crc32);
        }
            
        return stream.ToArray();
    }

    public FirmwareSegment? GetSegmentById(int id)
    {
        return Segments.FirstOrDefault(segment => segment.Id == id);
    }
        
    public override string ToString()
    {
        return "Magic=" + $"{Magic:X2}" + ", TotalSize=" + TotalSize + ", SegmentCount=" + SegmentsCount + $", CRC32=0x{Crc32:X2}";
    }

    public class FirmwareSegment
    {
        public long Id { get; }
        public long Crc32 { get; }
        public long Position { get; }
        public long Size { get; }
        public byte[] RawData { private set; get; }

        public FirmwareSegment(BinaryReader reader)
        {
            Id = reader.ReadInt32();
            Crc32 = reader.ReadInt32();
            Position = reader.ReadInt32();
            Size = reader.ReadInt32();

            try
            {
                using var _ = reader.BaseStream.ScopedSeek(Position, SeekOrigin.Begin);
                RawData = reader.ReadBytes((int)Size);
            }
            catch (IOException ex)
            {
                Log.E<FirmwareSegment>("Failed to read segment data");
                Log.E<FirmwareSegment>(ex.ToString());
                RawData = [];
            }
        }

        public override string ToString()
        {
            return "ID=" + Id +
                   ", Offset=0x" + $"{Position:X4}" +
                   ", Size=" + Size +
                   ", CRC32=0x" + $"{Crc32:X4}";
        }
    }
}