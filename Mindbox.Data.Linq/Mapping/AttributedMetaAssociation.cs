using System.Collections.ObjectModel;
using System.Data.Linq.SqlClient;

namespace System.Data.Linq.Mapping
{
	internal class AttributedMetaAssociation : MetaAssociationImpl 
	{
		private readonly AttributedMetaDataMember thisMember;
		private readonly MetaDataMember otherMember;
		private readonly ReadOnlyCollection<MetaDataMember> thisKey;
		private readonly ReadOnlyCollection<MetaDataMember> otherKey;
		private readonly MetaType otherType;
		private readonly bool isMany;
		private readonly bool isForeignKey;
		private readonly bool isUnique;
		private readonly bool isNullable;
		private readonly bool thisKeyIsPrimaryKey;
		private readonly bool otherKeyIsPrimaryKey;
		private readonly string deleteRule;
		private readonly bool deleteOnNull;


		internal AttributedMetaAssociation(AttributedMetaDataMember member, AssociationAttribute attr) 
		{
			thisMember = member;

			isMany = TypeSystem.IsSequenceType(thisMember.Type);
			var otherEntityType = isMany ? TypeSystem.GetElementType(thisMember.Type) : thisMember.Type;
			otherType = thisMember.DeclaringType.Model.GetMetaType(otherEntityType);
			thisKey = attr.ThisKey == null ? 
				thisMember.DeclaringType.IdentityMembers : 
				MakeKeys(thisMember.DeclaringType, attr.ThisKey);
			otherKey = attr.OtherKey == null ? otherType.IdentityMembers : MakeKeys(otherType, attr.OtherKey);
			thisKeyIsPrimaryKey = AreEqual(thisKey, thisMember.DeclaringType.IdentityMembers);
			otherKeyIsPrimaryKey = AreEqual(otherKey, otherType.IdentityMembers);
			isForeignKey = attr.IsForeignKey;

			isUnique = attr.IsUnique;
			deleteRule = attr.DeleteRule;
			deleteOnNull = attr.DeleteOnNull;

			// if any key members are not nullable, the association is not nullable
			isNullable = true;
			foreach (var thisKeyMember in thisKey) 
			{
				if (!thisKeyMember.CanBeNull) 
				{
					isNullable = false;
					break;
				}
			}

			// validate DeleteOnNull specification
			if (deleteOnNull && (!isForeignKey || isMany || isNullable))
				throw Error.InvalidDeleteOnNullSpecification(member);

			if ((thisKey.Count != otherKey.Count) && (thisKey.Count > 0) && (otherKey.Count > 0))
				throw Error.MismatchedThisKeyOtherKey(member.Name, member.DeclaringType.Name);

			// determine reverse reference member
			foreach (var otherTypePersistentMember in otherType.PersistentDataMembers)
			{
				var otherTypeMemberAssociationAttribute = ((AttributedMetaModel)thisMember.DeclaringType.Model)
					.TryGetAssociationAttribute(otherTypePersistentMember.Member);
				if ((otherTypeMemberAssociationAttribute != null) &&
					(otherTypePersistentMember != thisMember) && 
					(otherTypeMemberAssociationAttribute.Name == attr.Name))
				{
					otherMember = otherTypePersistentMember;
					break;
				}
			}

			//validate the number of ThisKey columns is the same as the number of OtherKey columns
		}


		public override MetaType OtherType 
		{
			get { return otherType; }
		}

		public override MetaDataMember ThisMember 
		{
			get { return thisMember; }
		}

		public override MetaDataMember OtherMember 
		{
			get { return otherMember; }
		}

		public override ReadOnlyCollection<MetaDataMember> ThisKey 
		{
			get { return thisKey; }
		}

		public override ReadOnlyCollection<MetaDataMember> OtherKey 
		{
			get { return otherKey; }
		}

		public override bool ThisKeyIsPrimaryKey 
		{
			get { return thisKeyIsPrimaryKey; }
		}

		public override bool OtherKeyIsPrimaryKey 
		{
			get { return otherKeyIsPrimaryKey; }
		}

		public override bool IsMany 
		{
			get { return isMany; }
		}

		public override bool IsForeignKey 
		{
			get { return isForeignKey; }
		}

		public override bool IsUnique 
		{
			get { return isUnique; }
		}

		public override bool IsNullable 
		{
			get { return isNullable; }
		}

		public override string DeleteRule 
		{
			get { return deleteRule; }
		}

		public override bool DeleteOnNull 
		{
			get { return deleteOnNull; }
		}
	}
}