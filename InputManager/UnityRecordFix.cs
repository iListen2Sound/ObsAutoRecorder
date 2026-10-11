using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// UnityRecordFix.cs
using System;

namespace System.Runtime.CompilerServices
{
	// Fixes the Record init-only constructor error in Unity
	public static class IsExternalInit { }

	// Fixes the specific NullableContextAttribute error you are seeing
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false)]
	public sealed class NullableContextAttribute : Attribute
	{
		public readonly byte Flag;
		public NullableContextAttribute(byte flag) => Flag = flag;
	}

	// Prevents potential cascade errors with other nullable properties
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
	public sealed class NullableAttribute : Attribute
	{
		public readonly byte[] Flags;
		public NullableAttribute(byte flag) => Flags = new[] { flag };
		public NullableAttribute(byte[] flags) => Flags = flags;
	}
}
