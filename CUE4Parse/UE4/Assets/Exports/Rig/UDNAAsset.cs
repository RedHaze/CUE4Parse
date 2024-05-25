using System;
using System.Buffers.Binary;
using System.Linq;
using System.Xml.Linq;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Assets.Exports.Rig
{
    public enum DNAFileVersion : uint
    {
        unknown = 0,
        v21 = (2 << 16) + 1, // rev(2, 1),
        v22 = (2 << 16) + 2, // rev(2, 2)
        v23 = (2 << 16) + 3, // rev(2, 3)
        latest = v23,
    }

    struct DNAVersion
    {
        public readonly ushort generation;
        public readonly ushort version;

        public DNAVersion(FAssetArchive Ar)
        {
            generation = BinaryPrimitives.ReadUInt16BigEndian(Ar.ReadBytes(2));
            version = BinaryPrimitives.ReadUInt16BigEndian(Ar.ReadBytes(2));
            // Aligned
        }

        public DNAFileVersion FileVersion()
        {
            return (DNAFileVersion)((generation << 16) + version);
        }
    }

    struct SectionLookupTable
    {
        public readonly uint descriptor;
        public readonly uint definition;
        public readonly uint behavior;
        public readonly uint controls;
        public readonly uint joints;
        public readonly uint blendShapeChannels;
        public readonly uint animatedMaps;
        public readonly uint geometry;

        public SectionLookupTable(FAssetArchive Ar)
        {
            descriptor = BinaryPrimitives.ReadUInt32BigEndian(Ar.ReadBytes(4));
            definition = BinaryPrimitives.ReadUInt32BigEndian(Ar.ReadBytes(4));
            behavior = BinaryPrimitives.ReadUInt32BigEndian(Ar.ReadBytes(4));
            controls = BinaryPrimitives.ReadUInt32BigEndian(Ar.ReadBytes(4));
            joints = BinaryPrimitives.ReadUInt32BigEndian(Ar.ReadBytes(4));
            blendShapeChannels = BinaryPrimitives.ReadUInt32BigEndian(Ar.ReadBytes(4));
            animatedMaps = BinaryPrimitives.ReadUInt32BigEndian(Ar.ReadBytes(4));
            geometry = BinaryPrimitives.ReadUInt32BigEndian(Ar.ReadBytes(4));
            // Aligned
        }
    }

    struct RawCoordinateSystem
    {
        public readonly ushort xAxis;
        public readonly ushort yAxis;
        public readonly ushort zAxis;

        public RawCoordinateSystem(FAssetArchive Ar)
        {
            xAxis = Ar.Read<ushort>();
            yAxis = Ar.Read<ushort>();
            zAxis = Ar.Read<ushort>();
            // Needs alignment here???
        }
    }

    struct RawDescriptor
    {
        public readonly string name;
        public readonly ushort archetype;
        public readonly ushort gender;
        public readonly ushort age;
        public readonly Tuple<string, string>[] metadata;
        public readonly ushort translationUnit;
        public readonly ushort rotationUnit;
        public readonly RawCoordinateSystem coordinateSystem;
        public readonly ushort lodCount;
        public readonly ushort maxLOD;
        public readonly string complexity;
        public readonly string dbName;

        public RawDescriptor(FAssetArchive Ar)
        {
            var result = Ar.ReadArray<char>();
            name = "ABA";
            archetype = Ar.Read<ushort>();
            gender = Ar.Read<ushort>();
            age = Ar.Read<ushort>();
            //metadata = Ar.ReadArray<Tuple<string, string>>()
        }
    }

    public class UDNAAsset : UObject
    {
        readonly public byte[] signature = { (byte)'D', (byte) 'N', (byte)'A' };
        readonly public byte[] eof = { (byte) 'A', (byte) 'N', (byte) 'D' };
        public DNAFileVersion DNAFileVersion = DNAFileVersion.unknown;
        public string DNAFileName { get; private set; }


        // Endianness - Big / Network
        // TSize = uint32t -- 4byte alignment
        // TOffset = uint32t
        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            DNAFileName = GetOrDefault<string>(nameof(DNAFileName));

            if (FDNAAssetCustomVersion.Get(Ar) >= FDNAAssetCustomVersion.Type.BeforeCustomVersionWasAdded)
            {
                // Signature
                if (!Ar.ReadBytes(3).SequenceEqual(signature))
                    return;

                // Force 4 byte alignment
                //Ar.ReadByte();

                // Version
                var version = new DNAVersion(Ar);
                DNAFileVersion = version.FileVersion();

                // v21
                // sections
                var sections = new SectionLookupTable(Ar);

                // descriptor
                //Ar.SeekAbsolute(sections.descriptor, System.IO.SeekOrigin.End);
                //var descriptor = new RawDescriptor(Ar);

                // definition

                // behavior

                // geometry

                // eof check
                if (!Ar.ReadBytes(3).SequenceEqual(eof))
                    Console.WriteLine("ERROR: invalid end of DNA file");
            }
        }
    }
}
