using System.Runtime.Serialization;
using System.Text;

namespace System.Data.Linq
{
	[DataContract]
	[Serializable]
	public sealed class Binary : IEquatable<Binary> 
	{
		public static implicit operator Binary(byte[] value)
		{
			return new Binary(value);
		}

		public static bool operator ==(Binary binary1, Binary binary2)
		{
			if (ReferenceEquals(binary1, binary2))
				return true;
			if (ReferenceEquals(binary1, null) || ReferenceEquals(binary2, null))
				return false;
			return binary1.EqualsTo(binary2);
		}

		public static bool operator !=(Binary binary1, Binary binary2)
		{
			return !(binary1 == binary2);
		}


		[DataMember(Name="Bytes")]
		private byte[] bytes;

		[NonSerialized]
		private int? hashCode;


		public Binary(byte[] value) 
		{
			if (value == null) 
			{
				bytes = new byte[0];
			}
			else 
			{
				bytes = new byte[value.Length];
				Array.Copy(value, bytes, value.Length);
			}
		}


		public int Length
		{
			get { return bytes.Length; }
		}


		public byte[] ToArray() 
		{
			var copy = new byte[bytes.Length];
			Array.Copy(bytes, copy, copy.Length);
			return copy;
		}

		public bool Equals(Binary other) 
		{
			return EqualsTo(other);
		}

		public override bool Equals(object obj) 
		{
			return EqualsTo(obj as Binary);
		}

		public override int GetHashCode() 
		{
			// hash code is not marked [DataMember], so when
			// using the DataContractSerializer, we'll need
			// to recompute the hash after deserialization.
			if (!hashCode.HasValue)
				ComputeHash();
			return hashCode.Value;
		}

		public override string ToString() 
		{
			return "\"" + Convert.ToBase64String(bytes, 0, bytes.Length) + "\"";
		}


		private bool EqualsTo(Binary binary) 
		{
			if (ReferenceEquals(this, binary))
				return true;
			if (ReferenceEquals(binary, null))
				return false;
			if (bytes.Length != binary.bytes.Length)
				return false;
			if (GetHashCode() != binary.GetHashCode())
				return false;
			for (var i = 0; i < bytes.Length; i++)
				if (bytes[i] != binary.bytes[i])
					return false;
			return true;
		}

		/// <summary>
		/// Simple hash using pseudo-random coefficients for each byte in 
		/// the array to achieve order dependency.
		/// </summary>
		private void ComputeHash() 
		{
			var currentHashCode = 0;
			var currentByteMultiplier = 314;
			foreach (var currentByte in bytes)
			{
				currentHashCode = currentHashCode * currentByteMultiplier + currentByte;
				currentByteMultiplier = currentByteMultiplier * 159;
			}
			hashCode = currentHashCode;
		}
	}
}