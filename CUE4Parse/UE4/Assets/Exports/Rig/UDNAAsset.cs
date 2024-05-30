using System;
using System.Buffers.Binary;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Objects;
using Serilog.Core;

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

        public DNAVersion(FArchiveBigEndian Ar)
        {
            generation = Ar.Read<ushort>();
            version = Ar.Read<ushort>();
        }

        public DNAFileVersion FileVersion()
        {
            return (DNAFileVersion)((generation << 16) + version);
        }
    }

    public struct SectionLookupTable
    {
        public readonly uint descriptor;
        public readonly uint definition;
        public readonly uint behavior;
        public readonly uint controls;
        public readonly uint joints;
        public readonly uint blendShapeChannels;
        public readonly uint animatedMaps;
        public readonly uint geometry;

        public readonly long offsetStart;

        public SectionLookupTable(FArchiveBigEndian Ar, long start)
        {
            descriptor = Ar.Read<uint>();
            definition = Ar.Read<uint>();
            behavior = Ar.Read<uint>();
            controls = Ar.Read<uint>();
            joints = Ar.Read<uint>();
            blendShapeChannels = Ar.Read<uint>();
            animatedMaps = Ar.Read<uint>();
            geometry = Ar.Read<uint>();

            offsetStart = start;
        }
    }

    public struct RawCoordinateSystem
    {
        public readonly ushort xAxis;
        public readonly ushort yAxis;
        public readonly ushort zAxis;

        public RawCoordinateSystem(FArchiveBigEndian Ar)
        {
            xAxis = Ar.Read<ushort>();
            yAxis = Ar.Read<ushort>();
            zAxis = Ar.Read<ushort>();
        }
    }

    public struct MetadataPair
    {
        public readonly string key;
        public readonly string value;

        public MetadataPair(FArchiveBigEndian Ar)
        {
            key = Ar.ReadString();
            value = Ar.ReadString();
        }
    }

    public struct RawDescriptor
    {
        public readonly string name;
        public readonly ushort archetype;
        public readonly ushort gender;
        public readonly ushort age;
        public readonly MetadataPair[] metadata;
        public readonly ushort translationUnit;
        public readonly ushort rotationUnit;
        public readonly RawCoordinateSystem coordinateSystem;
        public readonly ushort lodCount;
        public readonly ushort maxLOD;
        public readonly string complexity;
        public readonly string dbName;

        public RawDescriptor(FArchiveBigEndian Ar)
        {
            name = Ar.ReadString();
            archetype = Ar.Read<ushort>();
            gender = Ar.Read<ushort>();
            age = Ar.Read<ushort>();
            metadata = Ar.ReadArray(() => new MetadataPair(Ar));
            translationUnit = Ar.Read<ushort>();
            rotationUnit = Ar.Read<ushort>();
            // TODO: Ar.Read<RawCoordinateSystem>() results in WRONG VALUES, maybe do not extend from FArchive???
            coordinateSystem = new RawCoordinateSystem(Ar);
            lodCount = Ar.Read<ushort>();
            maxLOD = Ar.Read<ushort>();
            complexity = Ar.ReadString();
            dbName = Ar.ReadString();
        }
    }

    public struct RawLODMapping {
        public readonly ushort[] lods;
        public readonly ushort[][] indices;

        public RawLODMapping(FArchiveBigEndian Ar)
        {
            //archive.label("lods");
            lods = Ar.ReadArray(Ar.Read<ushort>);

            //archive.label("indices");
            indices = Ar.ReadArray(() => (Ar.ReadArray(Ar.Read<ushort>)));
        }

    }

    public struct RawVector3Vector
    {
        public readonly float[] xs;
        public readonly float[] ys;
        public readonly float[] zs;

        public RawVector3Vector(FArchiveBigEndian Ar)
        {
            xs = Ar.ReadArray(Ar.Read<float>);
            ys = Ar.ReadArray(Ar.Read<float>);
            zs = Ar.ReadArray(Ar.Read<float>);
        }
    }

    public struct RawSurjectiveMapping<TFrom, TTo> {
        public readonly TFrom[] from;
        public readonly TTo[] to;

        public RawSurjectiveMapping(FArchiveBigEndian Ar)
        {
            from = Ar.ReadArray(Ar.Read<TFrom>);
            to = Ar.ReadArray(Ar.Read<TTo>);
        }
    }

    public struct RawDefinition
    {
        public readonly RawLODMapping lodJointMapping;
        public readonly RawLODMapping lodBlendShapeMapping;
        public readonly RawLODMapping lodAnimatedMapMapping;
        public readonly RawLODMapping lodMeshMapping;
        public readonly string[] guiControlNames;
        public readonly string[] rawControlNames;
        public readonly string[] jointNames;
        public readonly string[] blendShapeChannelNames;
        public readonly string[] animatedMapNames;
        public readonly string[] meshNames;
        public readonly RawSurjectiveMapping<ushort, ushort> meshBlendShapeChannelMapping;
        public readonly ushort[] jointHierarchy;
        public readonly RawVector3Vector neutralJointTranslations;
        public readonly RawVector3Vector neutralJointRotations;

        public RawDefinition(FArchiveBigEndian Ar)
        {
            lodJointMapping = new RawLODMapping(Ar);
            lodBlendShapeMapping = new RawLODMapping(Ar);
            lodAnimatedMapMapping = new RawLODMapping(Ar);
            lodMeshMapping = new RawLODMapping(Ar);
            guiControlNames = Ar.ReadArray(Ar.ReadString);
            rawControlNames = Ar.ReadArray(Ar.ReadString);
            jointNames = Ar.ReadArray(Ar.ReadString);
            blendShapeChannelNames = Ar.ReadArray(Ar.ReadString);
            animatedMapNames = Ar.ReadArray(Ar.ReadString);
            meshNames = Ar.ReadArray(Ar.ReadString);
            meshBlendShapeChannelMapping = new RawSurjectiveMapping<ushort, ushort>(Ar);
            jointHierarchy = Ar.ReadArray(Ar.Read<ushort>);
            neutralJointTranslations = new RawVector3Vector(Ar);
            neutralJointRotations = new RawVector3Vector(Ar);
        }
    }

    public struct RawConditionalTable
    {
        public readonly ushort[] inputIndices;
        public readonly ushort[] outputIndices;
        public readonly float[] fromValues;
        public readonly float[] toValues;
        public readonly float[] slopeValues;
        public readonly float[] cutValues;

        public RawConditionalTable(FArchiveBigEndian Ar)
        {
            inputIndices = Ar.ReadArray(Ar.Read<ushort>);
            outputIndices = Ar.ReadArray(Ar.Read<ushort>);
            fromValues = Ar.ReadArray(Ar.Read<float>);
            toValues = Ar.ReadArray(Ar.Read<float>);
            slopeValues = Ar.ReadArray(Ar.Read<float>);
            cutValues = Ar.ReadArray(Ar.Read<float>);
        }
    }

    public struct RawPSDMatrix
    {
        public readonly ushort[] rows;
        public readonly ushort[] columns;
        public readonly float[] values;

        public RawPSDMatrix(FArchiveBigEndian Ar)
        {
            rows = Ar.ReadArray(Ar.Read<ushort>);
            columns = Ar.ReadArray(Ar.Read<ushort>);
            values = Ar.ReadArray(Ar.Read<float>);
        }
    }

    public struct RawControls
    {
        public readonly ushort psdCount;
        public readonly RawConditionalTable conditionals;
        public readonly RawPSDMatrix psds;

        public RawControls(FArchiveBigEndian Ar)
        {
            psdCount = Ar.Read<ushort>();
            conditionals = new RawConditionalTable(Ar);
            psds = new RawPSDMatrix(Ar);
        }
    }

    public struct RawJointGroups
    {
        public readonly ushort[] lods;
        public readonly ushort[] inputIndices;
        public readonly ushort[] outputIndices;
        public readonly float[] values;
        public readonly ushort[] jointInidices;

        public RawJointGroups(FArchiveBigEndian Ar)
        {
            lods = Ar.ReadArray(Ar.Read<ushort>);
            inputIndices = Ar.ReadArray(Ar.Read<ushort>);
            outputIndices = Ar.ReadArray(Ar.Read<ushort>);
            values = Ar.ReadArray(Ar.Read<float>);
            jointInidices = Ar.ReadArray(Ar.Read<ushort>);
        }
    }

    public struct RawJoints
    {
        public readonly ushort rowCount;
        public readonly ushort colCount;
        public readonly RawJointGroups[] jointGroups;

        public RawJoints(FArchiveBigEndian Ar)
        {
            rowCount = Ar.Read<ushort>();
            colCount = Ar.Read<ushort>();
            jointGroups = Ar.ReadArray(() => new RawJointGroups(Ar));
        }
    }

    public struct RawBlendShapeChannels
    {
        public readonly ushort[] lods;
        public readonly ushort[] inputIndices;
        public readonly ushort[] outputIndices;

        public RawBlendShapeChannels(FArchiveBigEndian Ar)
        {
            lods = Ar.ReadArray(Ar.Read<ushort>);
            inputIndices = Ar.ReadArray(Ar.Read<ushort>);
            outputIndices = Ar.ReadArray(Ar.Read<ushort>);
        }
    }

    public struct RawAnimatedMaps
    {
        public readonly ushort[] lods;
        public RawConditionalTable conditionals;

        public RawAnimatedMaps(FArchiveBigEndian Ar)
        {
            lods = Ar.ReadArray(Ar.Read<ushort>);
            conditionals = new RawConditionalTable(Ar);
        }
    }

    public struct RawBehavior
    {
        public readonly RawControls controls;
        public readonly RawJoints joints;
        public readonly RawBlendShapeChannels blendShapeChannels;
        public readonly RawAnimatedMaps animatedMaps;

        public RawBehavior(FArchiveBigEndian Ar)
        {
            controls = new RawControls(Ar);
            joints = new RawJoints(Ar);
            blendShapeChannels = new RawBlendShapeChannels(Ar);
            animatedMaps = new RawAnimatedMaps(Ar);
        }

        public RawBehavior(FArchiveBigEndian Ar, SectionLookupTable sections)
        {
            // TODO: Only seek based on condition of file version
            Ar.Seek(sections.offsetStart + sections.controls, System.IO.SeekOrigin.Begin);
            controls = new RawControls(Ar);

            Ar.Seek(sections.offsetStart + sections.joints, System.IO.SeekOrigin.Begin);
            joints = new RawJoints(Ar);

            Ar.Seek(sections.offsetStart + sections.blendShapeChannels, System.IO.SeekOrigin.Begin);
            blendShapeChannels = new RawBlendShapeChannels(Ar);

            Ar.Seek(sections.offsetStart + sections.animatedMaps, System.IO.SeekOrigin.Begin);
            animatedMaps = new RawAnimatedMaps(Ar);
        }
    }

    public class UDNAAsset : UObject
    {
        readonly public byte[] signature = { (byte)'D', (byte) 'N', (byte)'A' };
        readonly public byte[] eof = { (byte) 'A', (byte) 'N', (byte) 'D' };
        public DNAFileVersion DNAFileVersion = DNAFileVersion.unknown;
        public string DNAFileName { get; private set; }

        public RawDescriptor descriptor;
        public RawDefinition definition;
        public RawBehavior behavior;

        private void DeserializeDNA(FArchiveBigEndian Ar)
        {
            // Signature
            var offsetStart = Ar.Position;
            if (!Ar.ReadBytes(3).SequenceEqual(signature))
                return;

            // Version
            var version = new DNAVersion(Ar);
            DNAFileVersion = version.FileVersion();

            // v21
            // TODO: Other file versions
            // sections
            var sections = new SectionLookupTable(Ar, offsetStart);

            // descriptor
            // TODO: Only seek based on condition of file version
            Ar.Seek(sections.offsetStart + sections.descriptor, System.IO.SeekOrigin.Begin);
            descriptor = new RawDescriptor(Ar);

            // definition
            Ar.Seek(sections.offsetStart + sections.definition, System.IO.SeekOrigin.Begin);
            definition = new RawDefinition(Ar);

            // behavior
            Ar.Seek(sections.offsetStart + sections.behavior, System.IO.SeekOrigin.Begin);
            behavior = new RawBehavior(Ar);

            // geometry (unimplemented)
            // TODO: Implement geom parsing
            Ar.Seek(sections.offsetStart + sections.geometry, System.IO.SeekOrigin.Begin);
            Ar.ReadBytes(4);

            // eof check
            // TODO: convert to exception
            if (!Ar.ReadBytes(3).SequenceEqual(eof))
                Console.WriteLine("ERROR: invalid end of DNA file");
        }

        // Endianness - Big / Network
        // TSize = uint32t -- 4byte alignment
        // TOffset = uint32t
        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            DNAFileName = GetOrDefault<string>(nameof(DNAFileName));
            if (FDNAAssetCustomVersion.Get(Ar) >= FDNAAssetCustomVersion.Type.BeforeCustomVersionWasAdded)
            {
                DeserializeDNA(new FArchiveBigEndian(Ar));
            }
        }
    }
}
