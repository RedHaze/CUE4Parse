using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions
{
    public static class FCurveExpressionCustomVersion
    {
        public enum Type
        {
            BeforeCustomVersionWasAdded = 0,
            SerializedExpressions,
            ExpressionDataInSharedObject,
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1,
        }

        public static readonly FGuid GUID = new(0xA26D36AE, 0x26935388, 0xA8C5CB96, 0x2B95B4AF);

        public static Type Get(FArchive Ar)
        {
            var ver = Ar.CustomVer(GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                /* TODO: Figure out which versions go with which Type */
                _ => Type.LatestVersion
            };
        }
    }
}
