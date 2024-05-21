using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.HighPerformance;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FExpressionObject
    {
        [JsonConverter(typeof(EOperatorConverter))]
        public enum EOperator : int
        {
            Negate,             // Negation operator.
            Add,                // Add last two values on stack and add the result to the stack.
            Subtract,           // Same but subtract 
            Multiply,           // Same but multiply
            Divide,             // Same but divide (div-by-zero returns zero)
            Modulo,             // Same but modulo (mod-by-zero returns zero)
            Power,              // Raise to power
            FloorDivide,    	// Divide and round the result down
        }

        [JsonConverter(typeof(FFunctionRefConverter))]
        public struct FFunctionRef
        {
            FFunctionRef(int InIndex) { Index = InIndex; }
            public readonly int Index;
        };

        public enum OpElementType : int
        {
            OPERATOR,
            FNAME,
            FUNCTIONREF,
            FLOAT
        }

        public interface IOpElement { };

        public struct OpElementOperator : IOpElement {
            public EOperator value;

            public OpElementOperator(FArchive Ar)
            {
                value = Ar.Read<EOperator>();
            }
        }

        public struct OpElementFName: IOpElement
        {
            public FName value;

            public OpElementFName(FArchive Ar)
            {
                value = Ar.ReadFName();
            }
        }

        public struct OpElementFunctionRef : IOpElement
        {
            public FFunctionRef value;

            public OpElementFunctionRef(FArchive Ar)
            {
                value = Ar.Read<FFunctionRef>();
            }
        }

        public struct OpElementFloat : IOpElement
        {
            public float value;

            public OpElementFloat(FArchive Ar)
            {
                value = Ar.Read<float>();
            }
        }

        public readonly IOpElement[] Expression;

        public FExpressionObject(FArchive Ar)
        {
            List<IOpElement> expression = new List<IOpElement>();

            var operandCount = Ar.Read<int>();
            for(int i = 0; i < operandCount; i++)
            {
                OpElementType operandType = Ar.Read<OpElementType>();
                switch (operandType)
                {
                    case OpElementType.OPERATOR:
                        expression.Add(new OpElementOperator(Ar));
                        break;
                    case OpElementType.FNAME:
                        expression.Add(new OpElementFName(Ar));
                        break;
                    case OpElementType.FUNCTIONREF:
                        expression.Add(new OpElementFunctionRef(Ar));
                        break;
                    case OpElementType.FLOAT:
                        expression.Add(new OpElementFloat(Ar));
                        break;
                    default:
                        /* TODO: Raise exception */
                        Console.WriteLine("BROKEN");
                        break;
                }
            }

            Expression = expression.ToArray();
        }
    }
}
