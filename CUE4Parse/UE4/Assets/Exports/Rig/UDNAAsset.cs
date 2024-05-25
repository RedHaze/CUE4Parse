using System;
using System.Linq;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Assets.Exports.Rig
{


    struct DNAVersion
    {
        public readonly ushort generation;
        public readonly ushort version;

        enum DNAFileVersion : uint
        {
            unknown = 0,
            v21 = (2 << 16) + 1, // rev(2, 1),
            v22 = (2 << 16) + 2, // rev(2, 2)
            v23 = (2 << 16) + 3, // rev(2, 3)
            latest = v23,
        }

        public DNAVersion(FAssetArchive Ar)
        {
            //var data = Ar.Read<int>();
            //generation = (short)(data >> 16);
            //version = (short)(data & 0x0000FFFFu);
            var data = Ar.Read<uint>();
            Console.WriteLine(data);
            //generation = Ar.Read<ushort>();
            //version = Ar.Read<ushort>();
        }
    }

    public class UDNAAsset : UObject
    {
        readonly public byte[] signature = { (byte)'D', (byte) 'N', (byte)'A' };
        public string DNAFileName { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            DNAFileName = GetOrDefault<string>(nameof(DNAFileName));

            if (FDNAAssetCustomVersion.Get(Ar) >= FDNAAssetCustomVersion.Type.BeforeCustomVersionWasAdded)
            {
                // Signature
                if (!Ar.ReadBytes(3).SequenceEqual(signature))
                    return;

                // Version
                var version = new DNAVersion(Ar);
                Console.WriteLine(version.generation);

                // var magic = Ar.Read<uint>();
                // if (magic != 4279876)
                //     throw new Exception("invalid dna magic");
                //
                // var GetArchetype = Ar.Read<EArchetype>();
                // var GetGender = Ar.Read<EGender>();
                // var GetAge = Ar.Read<ushort>();
                // var GetMetaDataCount = Ar.Read<uint>();
                // for (int i = 0; i < GetMetaDataCount; i++)
                // {
                //     var key = Ar.ReadFString();
                //     var value = Ar.ReadFString();
                // }
                // Behavior
                // Geometry
            }
        }
    }
}
