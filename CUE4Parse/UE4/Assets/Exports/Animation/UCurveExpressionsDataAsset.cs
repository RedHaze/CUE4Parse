using System;
using System.Collections.Generic;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class UCurveExpressionsDataAsset : UObject
    {
        public FName[]? NamedConstants_DEPRECATED;
        public Dictionary<FName, FExpressionObject> ExpressionMap;

        public UCurveExpressionsDataAsset() {
            NamedConstants_DEPRECATED = null;
            ExpressionMap = new Dictionary<FName, FExpressionObject>();
        }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            if (FCurveExpressionCustomVersion.Get(Ar) < FCurveExpressionCustomVersion.Type.ExpressionDataInSharedObject)
            {
                NamedConstants_DEPRECATED = null;
            }
            else
            {
                NamedConstants_DEPRECATED = Ar.ReadArray(Ar.ReadFName);
            }

            var size = Ar.Read<int>();
            for(var i = 0; i < size; i++)
            {
                FName name = Ar.ReadFName();
                ExpressionMap[name] = new FExpressionObject(Ar);
            }

            Console.WriteLine("hi");
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            if (NamedConstants_DEPRECATED != null)
            {
                writer.WritePropertyName(nameof(NamedConstants_DEPRECATED));
                writer.WriteStartArray();
                foreach (var name in NamedConstants_DEPRECATED)
                    serializer.Serialize(writer, name);
                writer.WriteEndArray();
            }

            writer.WritePropertyName(nameof(ExpressionMap));
            writer.WriteStartObject();
            foreach (var (name, expression) in ExpressionMap) {
                writer.WritePropertyName(name.Text);
                serializer.Serialize(writer, expression);
            }
            writer.WriteEndObject();
        }
    }
}
