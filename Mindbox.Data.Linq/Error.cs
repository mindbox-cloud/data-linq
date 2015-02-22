using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using System.Data.Linq.SqlClient;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml;

namespace System.Data.Linq {
    /// <summary>
    /// DLinq-providerbase-specific custom exception factory.
    /// </summary>
    internal class Error {
        /// <summary>
        /// Exception thrown when a query cannot execute against a particular SQL server version.
        /// </summary>
        static internal Exception ExpressionNotSupportedForSqlServerVersion(Collection<string> reasons) {
            StringBuilder exceptionMessage = new StringBuilder("CannotTranslateExpressionToSql: ");
            foreach (string reason in reasons) {
                exceptionMessage.AppendLine(reason);                    
            }
            return new NotSupportedException(exceptionMessage.ToString());
        }

	    public static Exception ArgumentNull(string sequence)
	    {
		    return new ArgumentNullException(sequence);
	    }

	    public static Exception EntitySetDataBindingWithAbstractBaseClass(string name)
	    {
			return new InvalidOperationException("EntitySetDataBindingWithAbstractBaseClass: " + name + ".");
	    }

	    public static Exception EntitySetDataBindingWithNonPublicDefaultConstructor(string name)
	    {
			return new InvalidOperationException("EntitySetDataBindingWithNonPublicDefaultConstructor: " + name + ".");
	    }

	    public static Exception InvalidFieldInfo(Type objectType, Type fieldType, FieldInfo fi)
	    {
			return new InvalidOperationException("InvalidFieldInfo: " + objectType + ", " + fieldType + ", " + fi + ".");
	    }

	    public static Exception ArgumentWrongValue(string rowtype)
	    {
			return new ArgumentException("ArgumentWrongValue: " + rowtype + ".");
	    }

	    public static Exception UnexpectedNode(SqlNodeType nodeType)
	    {
			return new InvalidOperationException("UnexpectedNode: " + nodeType + ".");
	    }

		public static Exception UnexpectedNode(SqlNode node)
		{
			return new InvalidOperationException("UnexpectedNode: " + node + ".");
		}

		public static Exception ColumnReferencedIsNotInScope(string getColumnName)
	    {
			return new InvalidOperationException("ColumnReferencedIsNotInScope: " + getColumnName + ".");
	    }

	    public static Exception CannotAddChangeConflicts()
	    {
			return new InvalidOperationException("CannotAddChangeConflicts");
	    }

	    public static Exception ExpectedQueryableArgument(string expression, Type qType)
	    {
			return new InvalidOperationException("ExpectedQueryableArgument: " + expression + ", " + qType + ".");
	    }

	    public static Exception ColumnClrTypeDoesNotAgreeWithExpressionsClrType()
	    {
			return new InvalidOperationException("ColumnClrTypeDoesNotAgreeWithExpressionsClrType");
	    }

	    public static Exception ProviderTypeNotFound(string provider)
	    {
			return new InvalidOperationException("ProviderTypeNotFound: " + provider + ".");
	    }

	    public static Exception ArgumentOutOfRange(string mi)
	    {
		    return new ArgumentOutOfRangeException(mi);
	    }

	    public static Exception ClassLiteralsNotAllowed(Type clrType)
	    {
			return new InvalidOperationException("ClassLiteralsNotAllowed: " + clrType + ".");
	    }

	    public static Exception CouldNotHandleAliasRef(SqlNodeType nodeType)
	    {
			return new InvalidOperationException("CouldNotHandleAliasRef: " + nodeType + ".");
	    }

	    public static Exception EmptyCaseNotSupported()
	    {
			return new InvalidOperationException("EmptyCaseNotSupported");
	    }

	    public static Exception ExpectedPredicateFoundBit()
	    {
			return new InvalidOperationException("ExpectedPredicateFoundBit");
	    }

	    public static Exception ExpectedBitFoundPredicate()
	    {
			return new InvalidOperationException("ExpectedBitFoundPredicate");
	    }

	    public static Exception CouldNotFindTypeFromMapping(string name)
	    {
			return new InvalidOperationException("CouldNotFindTypeFromMapping: " + name + ".");
	    }

	    public static Exception ConvertToCharFromBoolNotSupported()
	    {
			return new InvalidOperationException("ConvertToCharFromBoolNotSupported");
	    }

	    public static Exception CreateDatabaseFailedBecauseOfClassWithNoMembers(Type type)
	    {
			return new InvalidOperationException("CreateDatabaseFailedBecauseOfClassWithNoMembers: " + type + ".");
	    }

	    public static Exception MethodHasNoSupportConversionToSql(object x)
	    {
			return new InvalidOperationException("MethodHasNoSupportConversionToSql: " + x + ".");
	    }

	    public static Exception CannotAssignNull(Type type)
	    {
			return new InvalidOperationException("CannotAssignNull: " + type + ".");
	    }

	    public static Exception UnsafeStringConversion(string toQueryString, string p1)
	    {
			return new InvalidOperationException("UnsafeStringConversion: " + toQueryString + ", " + p1 + ".");
	    }

	    public static Exception InsertAutoSyncFailure()
	    {
			return new InvalidOperationException("InsertAutoSyncFailure");
	    }

	    public static Exception CouldNotCreateAccessorToProperty(Type objectType, Type propertyType, PropertyInfo pi)
	    {
			return new InvalidOperationException(
				"CouldNotCreateAccessorToProperty: " + objectType + ", " + propertyType + ", " + pi + ".");
	    }

	    public static Exception CannotAttachAddNonNewEntities()
	    {
			return new InvalidOperationException("CannotAttachAddNonNewEntities");
	    }

	    public static Exception InvalidMethodExecution(string name)
	    {
			return new InvalidOperationException("InvalidMethodExecution: " + name + ".");
	    }

	    public static Exception MappedTypeMustHaveDefaultConstructor(Type type)
	    {
			return new InvalidOperationException("MappedTypeMustHaveDefaultConstructor: " + type + ".");
	    }

	    public static Exception TransactionDoesNotMatchConnection()
	    {
			return new InvalidOperationException("TransactionDoesNotMatchConnection");
	    }

	    public static Exception InvalidFormatNode(object x)
	    {
			return new InvalidOperationException("InvalidFormatNode: " + x + ".");
	    }

		public static Exception UnexpectedFloatingColumn()
	    {
			return new InvalidOperationException("UnexpectedFloatingColumn");
	    }

	    public static Exception UnexpectedTypeCode(object x)
	    {
			return new InvalidOperationException("UnexpectedTypeCode: " + x + ".");
	    }

	    public static Exception UnexpectedSharedExpression()
	    {
			return new InvalidOperationException("UnexpectedSharedExpression");
	    }

	    public static Exception UnexpectedSharedExpressionReference()
	    {
			return new InvalidOperationException("UnexpectedSharedExpressionReference");
	    }

	    public static Exception MethodCannotBeFound(string methodName)
	    {
			return new InvalidOperationException("MethodCannotBeFound: " + methodName + ".");
	    }

	    public static Exception IncludeNotAllowedAfterFreeze()
	    {
			return new InvalidOperationException("IncludeNotAllowedAfterFreeze");
	    }

	    public static Exception NoMethodInTypeMatchingArguments(Type type)
	    {
			return new InvalidOperationException("NoMethodInTypeMatchingArguments: " + type + ".");
	    }

	    public static Exception SubqueryNotAllowedAfterFreeze()
	    {
			return new InvalidOperationException("SubqueryNotAllowedAfterFreeze");
	    }

	    public static Exception UnhandledStringTypeComparison()
	    {
			return new InvalidOperationException("UnhandledStringTypeComparison");
	    }

	    public static Exception DataContextCannotBeUsedAfterDispose()
	    {
			return new InvalidOperationException("DataContextCannotBeUsedAfterDispose");
	    }

	    public static Exception UnableToAssignValueToReadonlyProperty(PropertyInfo pi)
	    {
			return new InvalidOperationException("UnableToAssignValueToReadonlyProperty: " + pi + ".");
	    }

	    public static Exception DatabaseNodeNotFound(string httpSchemasMicrosoftComLinqtosqlMapping)
	    {
			return new InvalidOperationException("DatabaseNodeNotFound: " + httpSchemasMicrosoftComLinqtosqlMapping + ".");
	    }

	    public static Exception UnrecognizedExpressionNode(object x)
	    {
			return new InvalidOperationException("UnrecognizedExpressionNode: " + x + ".");
	    }

	    public static Exception InvalidUseOfGenericMethodAsMappedFunction(string name)
	    {
			return new InvalidOperationException("InvalidUseOfGenericMethodAsMappedFunction: " + name + ".");
	    }

	    public static Exception InvalidGroupByExpressionType(string name)
	    {
			return new InvalidOperationException("InvalidGroupByExpressionType: " + name + ".");
	    }

	    public static Exception InvalidLoadOptionsLoadMemberSpecification()
	    {
			return new InvalidOperationException("InvalidLoadOptionsLoadMemberSpecification");
	    }

	    public static Exception ProviderTypeNull()
	    {
			return new InvalidOperationException("ProviderTypeNull");
	    }

	    public static Exception ProviderDoesNotImplementRequiredInterface(Type providerType, Type type)
	    {
			return new InvalidOperationException(
				"ProviderDoesNotImplementRequiredInterface: " + providerType + ", " + type + ".");
	    }

	    public static Exception CouldNotConvert(Type fromType, Type toType)
	    {
			return new InvalidOperationException("CouldNotConvert: " + fromType + ", " + toType + ".");
	    }

	    public static Exception ColumnIsDefinedInMultiplePlaces(string getColumnName)
	    {
			return new InvalidOperationException("ColumnIsDefinedInMultiplePlaces: " + getColumnName + ".");
	    }

	    public static Exception UnionIncompatibleConstruction()
	    {
			return new InvalidOperationException("UnionIncompatibleConstruction");
	    }

	    public static Exception UnionDifferentMembers()
	    {
			return new InvalidOperationException("UnionDifferentMembers");
	    }

	    public static Exception UnionDifferentMemberOrder()
	    {
			return new InvalidOperationException("UnionDifferentMemberOrder");
	    }

	    public static Exception UnhandledExpressionType(object x)
	    {
			return new InvalidOperationException("UnhandledExpressionType: " + x + ".");
	    }

	    public static Exception CouldNotFindRequiredAttribute(string attribute, string readOuterXml)
	    {
			return new InvalidOperationException("CouldNotFindRequiredAttribute: " + attribute + ", " + readOuterXml + ".");
	    }

	    public static Exception LinkAlreadyLoaded()
	    {
			return new InvalidOperationException("LinkAlreadyLoaded");
	    }

	    public static Exception UnsupportedNodeType(ExpressionType nodeType)
	    {
			return new InvalidOperationException("UnsupportedNodeType: " + nodeType + ".");
	    }

	    public static Exception UnhandledBindingType(MemberBindingType bindingType)
	    {
			return new InvalidOperationException("UnhandledBindingType: " + bindingType + ".");
	    }

	    public static Exception UnableToResolveRootForType(Type type)
	    {
			return new InvalidOperationException("UnableToResolveRootForType: " + type + ".");
	    }

	    public static Exception CouldNotDetermineDbGeneratedSqlType(Type type)
	    {
			return new InvalidOperationException("CouldNotDetermineDbGeneratedSqlType: " + type + ".");
	    }

	    public static Exception IncludeCycleNotAllowed()
	    {
			return new InvalidOperationException("IncludeCycleNotAllowed");
	    }

	    public static Exception InvalidGroupByExpression()
	    {
			return new InvalidOperationException("InvalidGroupByExpression");
	    }

	    public static Exception ExpectedEmptyElement(string nodeName, XmlNodeType nodeType, string name)
	    {
			return new InvalidOperationException("ExpectedEmptyElement: " + nodeName + ", " + nodeType + ", " + name + ".");
	    }

	    public static Exception InvalidOrderByExpression(string toQueryString)
	    {
			return new InvalidOperationException("InvalidOrderByExpression: " + toQueryString + ".");
	    }

	    public static Exception SubqueryNotSupportedOn(MemberInfo mi)
	    {
			return new InvalidOperationException("SubqueryNotSupportedOn: " + mi + ".");
	    }

	    public static Exception SubqueryNotSupportedOnType(string name, Type declaringType)
	    {
			return new InvalidOperationException("SubqueryNotSupportedOnType: " + name + ", " + declaringType + ".");
	    }

	    public static Exception SubqueryMustBeSequence()
	    {
			return new InvalidOperationException("SubqueryMustBeSequence");
	    }

	    public static Exception RefreshOfDeletedObject()
	    {
			return new InvalidOperationException("RefreshOfDeletedObject");
	    }

	    public static Exception OptionsCannotBeModifiedAfterQuery()
	    {
			return new InvalidOperationException("OptionsCannotBeModifiedAfterQuery");
	    }

	    public static Exception SubqueryDoesNotSupportOperator(string name)
	    {
			return new InvalidOperationException("SubqueryDoesNotSupportOperator: " + name + ".");
	    }

	    public static Exception EntityRefAlreadyLoaded()
	    {
			return new InvalidOperationException("EntityRefAlreadyLoaded");
	    }

	    public static Exception InvalidReferenceToRemovedAliasDuringDeflation()
	    {
			return new InvalidOperationException("InvalidReferenceToRemovedAliasDuringDeflation");
	    }

	    public static Exception ColumnIsNotAccessibleThroughDistinct(string getColumnName)
	    {
			return new InvalidOperationException("ColumnIsNotAccessibleThroughDistinct: " + getColumnName + ".");
	    }

	    public static Exception UnrecognizedElement(string format)
	    {
			return new InvalidOperationException("UnrecognizedElement: " + format + ".");
	    }

	    public static Exception UnexpectedElement(string database, string format)
	    {
			return new InvalidOperationException("UnexpectedElement: " + database + ", " + format + ".");
	    }

	    public static Exception ColumnIsNotAccessibleThroughGroupBy(string getColumnName)
	    {
			return new InvalidOperationException("ColumnIsNotAccessibleThroughGroupBy: " + getColumnName + ".");
	    }

	    public static Exception CouldNotDetermineSqlType(Type type)
	    {
			return new InvalidOperationException("CouldNotDetermineSqlType: " + type + ".");
	    }

	    public static Exception DeferredLoadingRequiresObjectTracking()
	    {
			return new InvalidOperationException("DeferredLoadingRequiresObjectTracking");
	    }

	    public static Exception IQueryableCannotReturnSelfReferencingConstantExpression()
	    {
			return new InvalidOperationException("IQueryableCannotReturnSelfReferencingConstantExpression");
	    }

	    public static Exception CapturedValuesCannotBeSequences()
	    {
			return new InvalidOperationException("CapturedValuesCannotBeSequences");
	    }

	    public static Exception ConstructedArraysNotSupported()
	    {
			return new InvalidOperationException("ConstructedArraysNotSupported");
	    }

	    public static Exception ParametersCannotBeSequences()
	    {
			return new InvalidOperationException("ParametersCannotBeSequences");
	    }

	    public static Exception UnrecognizedAttribute(string format)
	    {
			return new InvalidOperationException("UnrecognizedAttribute: " + format + ".");
	    }

	    public static Exception CannotCompareItemsAssociatedWithDifferentTable()
	    {
			return new InvalidOperationException("CannotCompareItemsAssociatedWithDifferentTable");
	    }

	    public static Exception ObjectTrackingRequired()
	    {
			return new InvalidOperationException("ObjectTrackingRequired");
	    }

	    public static Exception QueryWasCompiledForDifferentMappingSource()
	    {
			return new InvalidOperationException("QueryWasCompiledForDifferentMappingSource");
	    }

	    public static Exception ContextNotInitialized()
	    {
			return new InvalidOperationException("ContextNotInitialized");
	    }

	    public static Exception IifReturnTypesMustBeEqual(string name, string s)
	    {
			return new InvalidOperationException("IifReturnTypesMustBeEqual: " + name + ", " + s + ".");
	    }

	    public static Exception UnionWithHierarchy()
	    {
			return new InvalidOperationException("UnionWithHierarchy");
	    }

	    public static Exception DatabaseDeleteThroughContext()
	    {
			return new InvalidOperationException("DatabaseDeleteThroughContext");
	    }

	    public static Exception CannotPerformOperationDuringSubmitChanges()
	    {
			return new InvalidOperationException("CannotPerformOperationDuringSubmitChanges");
	    }

	    public static Exception ArgumentWrongType(object x, object y, object z)
	    {
			return new InvalidOperationException("ArgumentWrongType: " + x + ", " + y + ", " + z + ".");
	    }

	    public static Exception IdentityChangeNotAllowed(string name, string s)
	    {
			return new InvalidOperationException("IdentityChangeNotAllowed: " + name + ", " + s + ".");
	    }

	    public static Exception CannotPerformOperationOutsideSubmitChanges()
	    {
			return new InvalidOperationException("CannotPerformOperationOutsideSubmitChanges");
	    }

	    public static Exception DbGeneratedChangeNotAllowed(string name, string s)
	    {
			return new InvalidOperationException("DbGeneratedChangeNotAllowed: " + name + ", " + s + ".");
	    }

	    public static Exception ProviderNotInstalled(string dbName, string sqlCeProviderInvariantName)
	    {
			return new InvalidOperationException("ProviderNotInstalled: " + dbName + ", " + sqlCeProviderInvariantName + ".");
	    }

	    public static Exception ComparisonNotSupportedForType(Type clrType)
	    {
			return new InvalidOperationException("ComparisonNotSupportedForType: " + clrType + ".");
	    }

	    public static Exception TypeIsNotMarkedAsTable(Type type)
	    {
			return new InvalidOperationException("TypeIsNotMarkedAsTable: " + type + ".");
	    }

	    public static Exception CouldNotGetTableForSubtype(Type type, Type type1)
	    {
			return new InvalidOperationException("CouldNotGetTableForSubtype: " + type + ", " + type1 + ".");
	    }

	    public static Exception BadParameterType(Type type)
	    {
			return new InvalidOperationException("BadParameterType: " + type + ".");
	    }

	    public static Exception InvalidConnectionArgument(string connection)
	    {
			return new InvalidOperationException("InvalidConnectionArgument: " + connection + ".");
	    }

	    public static Exception NoDiscriminatorFound(object x)
	    {
			return new InvalidOperationException("NoDiscriminatorFound: " + x + ".");
	    }

	    public static Exception DiscriminatorClrTypeNotSupported(string name, string s, Type type)
	    {
			return new InvalidOperationException("DiscriminatorClrTypeNotSupported: " + name + ", " + s + ", " + type + ".");
	    }

	    public static Exception NonEntityAssociationMapping(Type type, string name, Type p2)
	    {
			return new InvalidOperationException("NonEntityAssociationMapping: " + type + ", " + name + ", " + p2 + ".");
	    }

	    public static Exception InheritanceTypeDoesNotDeriveFromRoot(Type type, Type type1)
	    {
			return new InvalidOperationException("InheritanceTypeDoesNotDeriveFromRoot: " + type + ", " + type1 + ".");
	    }

	    public static Exception AbstractClassAssignInheritanceDiscriminator(Type type)
	    {
			return new InvalidOperationException("AbstractClassAssignInheritanceDiscriminator: " + type + ".");
	    }

	    public static Exception InheritanceCodeMayNotBeNull()
	    {
			return new InvalidOperationException("InheritanceCodeMayNotBeNull");
	    }

	    public static Exception InheritanceTypeHasMultipleDiscriminators(object x)
	    {
			return new InvalidOperationException("InheritanceTypeHasMultipleDiscriminators: " + x + ".");
	    }

	    public static Exception InheritanceCodeUsedForMultipleTypes(object codeValue)
	    {
			return new InvalidOperationException("InheritanceCodeUsedForMultipleTypes: " + codeValue + ".");
	    }

	    public static Exception InheritanceTypeHasMultipleDefaults(object x)
	    {
			return new InvalidOperationException("InheritanceTypeHasMultipleDefaults: " + x + ".");
	    }

	    public static Exception InheritanceHierarchyDoesNotDefineDefault(Type type)
	    {
			return new InvalidOperationException("InheritanceHierarchyDoesNotDefineDefault: " + type + ".");
	    }

	    public static Exception BadProjectionInSelect()
	    {
			return new InvalidOperationException("BadProjectionInSelect");
	    }

	    public static Exception EntitySetAlreadyLoaded()
	    {
			return new InvalidOperationException("EntitySetAlreadyLoaded");
	    }

	    public static Exception NonInheritanceClassHasDiscriminator(MetaType type)
	    {
			return new InvalidOperationException("NonInheritanceClassHasDiscriminator: " + type + ".");
	    }

	    public static Exception InheritanceSubTypeIsAlsoRoot(Type type)
	    {
			return new InvalidOperationException("InheritanceSubTypeIsAlsoRoot: " + type + ".");
	    }

	    public static Exception ModifyDuringAddOrRemove()
	    {
			return new InvalidOperationException("ModifyDuringAddOrRemove");
	    }

	    public static Exception MemberMappedMoreThanOnce(string name)
	    {
			return new InvalidAsynchronousStateException("MemberMappedMoreThanOnce: " + name + ".");
	    }

	    public static Exception ProviderCannotBeUsedAfterDispose()
	    {
			return new InvalidOperationException("ProviderCannotBeUsedAfterDispose");
	    }

	    public static Exception CouldNotFindRuntimeTypeForMapping(string name)
	    {
			return new InvalidOperationException("CouldNotFindRuntimeTypeForMapping: " + name + ".");
	    }

	    public static Exception EntitySetModifiedDuringEnumeration()
	    {
			return new InvalidOperationException("EntitySetModifiedDuringEnumeration");
	    }

	    public static Exception CreateDatabaseFailedBecauseSqlCEDatabaseAlreadyExists(string dbName)
	    {
			return new InvalidOperationException("CreateDatabaseFailedBecauseSqlCEDatabaseAlreadyExists: " + dbName + ".");
	    }

	    public static Exception PrimaryKeyInSubTypeNotSupported(string name, string s)
	    {
			return new InvalidOperationException("PrimaryKeyInSubTypeNotSupported: " + name + ", " + s + ".");
	    }

	    public static Exception SqlMethodOnlyForSql(MethodBase getCurrentMethod)
	    {
			return new InvalidOperationException("SqlMethodOnlyForSql: " + getCurrentMethod + ".");
	    }

	    public static Exception InconsistentAssociationAndKeyChange(string name, string s)
	    {
			return new InvalidOperationException("InconsistentAssociationAndKeyChange: " + name + ", " + s + ".");
	    }

	    public static Exception CouldNotDetermineCatalogName()
	    {
			return new InvalidOperationException("CouldNotDetermineCatalogName");
	    }

	    public static Exception UnrecognizedRefreshObject()
	    {
			return new InvalidAsynchronousStateException("UnrecognizedRefreshObject");
	    }

	    public static Exception CouldNotRemoveRelationshipBecauseOneSideCannotBeNull(string name, string s, StringBuilder keys)
	    {
			return new InvalidOperationException(
				"CouldNotRemoveRelationshipBecauseOneSideCannotBeNull: " + name + ", " + s + ", " + keys + ".");
	    }

	    public static Exception CreateDatabaseFailedBecauseOfContextWithNoTables(string databaseName)
	    {
			return new InvalidOperationException("CreateDatabaseFailedBecauseOfContextWithNoTables: " + databaseName + ".");
	    }

	    public static Exception RefreshOfNewObject()
	    {
			return new InvalidOperationException("RefreshOfNewObject");
	    }

	    public static Exception TypeBinaryOperatorNotRecognized()
	    {
			return new InvalidAsynchronousStateException("TypeBinaryOperatorNotRecognized");
	    }

	    public static Exception CannotChangeInheritanceType(
			object dbDiscriminator, 
			object currentDiscriminator, 
			string name, 
			MetaType currentTypeFromDiscriminator)
	    {
			return new InvalidOperationException("CannotChangeInheritanceType: " + dbDiscriminator + ", " + 
				currentDiscriminator + ", " + name + ", " + currentTypeFromDiscriminator + ".");
	    }

	    public static Exception DidNotExpectTypeBinding()
	    {
			return new InvalidOperationException("DidNotExpectTypeBinding");
	    }

	    public static Exception DidNotExpectAs(UnaryExpression unaryExpression)
	    {
			return new InvalidOperationException("DidNotExpectAs: " + unaryExpression + ".");
	    }

	    public static Exception CycleDetected()
	    {
			return new InvalidOperationException("CycleDetected");
	    }

	    public static Exception TwoMembersMarkedAsPrimaryKeyAndDBGenerated(MemberInfo member, MemberInfo memberInfo)
	    {
			return new InvalidOperationException(
				"TwoMembersMarkedAsPrimaryKeyAndDBGenerated: " + member + ", " + memberInfo + ".");
	    }

	    public static Exception IdentityClrTypeNotSupported(MetaType declaringType, string name, Type type)
	    {
			return new InvalidOperationException(
				"IdentityClrTypeNotSupported: " + declaringType + ", " + name + ", " + type + ".");
	    }

	    public static Exception TwoMembersMarkedAsRowVersion(MemberInfo member, MemberInfo memberInfo)
	    {
			return new InvalidAsynchronousStateException("TwoMembersMarkedAsRowVersion: " + member + ", " + memberInfo + ".");
	    }

	    public static Exception TwoMembersMarkedAsInheritanceDiscriminator(MemberInfo member, MemberInfo memberInfo)
	    {
			return new InvalidOperationException(
				"TwoMembersMarkedAsInheritanceDiscriminator: " + member + ", " + memberInfo + ".");
	    }

	    public static Exception MappedMemberHadNoCorrespondingMemberInType(string memberName, string name)
	    {
			return new InvalidOperationException("MappedMemberHadNoCorrespondingMemberInType: " + memberName + ", " + name + ".");
	    }

	    public static Exception ExpectedClrTypesToAgree(Type newClrType, Type clrType)
	    {
			return new InvalidOperationException("ExpectedClrTypesToAgree: " + newClrType + ", " + clrType + ".");
	    }

	    public static Exception UnsupportedStringConstructorForm()
	    {
			return new InvalidOperationException("UnsupportedStringConstructorForm");
	    }

	    public static Exception CouldNotGetClrType()
	    {
			return new InvalidOperationException("CouldNotGetClrType");
	    }

	    public static Exception CouldNotGetSqlType()
	    {
			return new InvalidOperationException("CouldNotGetSqlType");
	    }

	    public static Exception VbLikeDoesNotSupportMultipleCharacterRanges()
	    {
			return new InvalidOperationException("VbLikeDoesNotSupportMultipleCharacterRanges");
	    }

	    public static Exception CouldNotTranslateExpressionForReading(Expression sourceExpression)
	    {
			return new InvalidOperationException("CouldNotTranslateExpressionForReading: " + sourceExpression + ".");
	    }

	    public static Exception UnsupportedDateTimeConstructorForm()
	    {
			return new InvalidOperationException("UnsupportedDateTimeConstructorForm");
	    }

	    public static Exception WrongDataContext()
	    {
			return new InvalidOperationException("WrongDataContext");
	    }

	    public static Exception CannotGetInheritanceDefaultFromNonInheritanceClass()
	    {
			return new InvalidOperationException("CannotGetInheritanceDefaultFromNonInheritanceClass");
	    }

	    public static Exception MappingOfInterfacesMemberIsNotSupported(string name, string s)
	    {
			return new InvalidOperationException("MappingOfInterfacesMemberIsNotSupported: " + name + ", " + s + ".");
	    }

	    public static Exception UnmappedClassMember(string name, string s)
	    {
			return new InvalidOperationException("UnmappedClassMember: " + name + ", " + s + ".");
	    }

	    public static Exception CannotMaterializeEntityType(Type type)
	    {
			return new InvalidOperationException("CannotMaterializeEntityType: " + type + ".");
	    }

	    public static Exception CannotPerformOperationForUntrackedObject()
	    {
			return new InvalidOperationException("CannotPerformOperationForUntrackedObject");
	    }

	    public static Exception VbLikeUnclosedBracket()
	    {
			return new InvalidOperationException("VbLikeUnclosedBracket");
	    }

	    public static Exception UnsupportedDateTimeOffsetConstructorForm()
	    {
			return new InvalidOperationException("UnsupportedDateTimeOffsetConstructorForm");
	    }

	    public static Exception MemberNotPartOfProjection(Type declaringType, string name)
	    {
			return new InvalidOperationException("MemberNotPartOfProjection: " + declaringType + ", " + name + ".");
	    }

	    public static Exception CannotMaterializeList(Type clrType)
	    {
			return new InvalidOperationException("CannotMaterializeList: " + clrType + ".");
	    }

	    public static Exception InvalidProviderType(string typeName)
	    {
			return new InvalidOperationException("InvalidProviderType: " + typeName + ".");
	    }

	    public static Exception NoResultTypesDeclaredForFunction(string name)
	    {
			return new InvalidOperationException("NoResultTypesDeclaredForFunction: " + name + ".");
	    }

	    public static Exception TooManyResultTypesDeclaredForFunction(string name)
	    {
			return new InvalidOperationException("TooManyResultTypesDeclaredForFunction: " + name + ".");
	    }

	    public static Exception UnsupportedTimeSpanConstructorForm()
	    {
			return new InvalidOperationException("UnsupportedTimeSpanConstructorForm");
	    }

	    public static Exception CouldNotConvertToPropertyOrField(MemberInfo mi)
	    {
			return new InvalidOperationException("CouldNotConvertToPropertyOrField: " + mi + ".");
	    }

	    public static Exception LoadOptionsChangeNotAllowedAfterQuery()
	    {
			return new InvalidOperationException("LoadOptionsChangeNotAllowedAfterQuery");
	    }

	    public static Exception ToStringOnlySupportedForPrimitiveTypes()
	    {
			return new InvalidOperationException("ToStringOnlySupportedForPrimitiveTypes");
	    }

	    public static Exception BadStorageProperty(string storageMemberName, Type declaringType, string name)
	    {
			return new InvalidOperationException(
				"BadStorageProperty: " + storageMemberName + ", " + declaringType + ", " + name + ".");
	    }

	    public static Exception IncorrectAutoSyncSpecification(string name)
	    {
			return new InvalidOperationException("IncorrectAutoSyncSpecification: " + name + ".");
	    }

	    public static Exception MethodFormHasNoSupportConversionToSql(string name, MethodInfo method)
	    {
			return new InvalidOperationException("MethodFormHasNoSupportConversionToSql: " + name + ", " + method + ".");
	    }

	    public static Exception UnmappedDataMember(MemberInfo mi, Type declaringType, MetaType type)
	    {
			return new InvalidOperationException("UnmappedDataMember: " + mi + ", " + declaringType + ", " + type + ".");
	    }

	    public static Exception DeferredMemberWrongType()
	    {
			return new InvalidOperationException("DeferredMemberWrongType");
	    }

	    public static Exception SkipNotSupportedForSequenceTypes()
	    {
			return new InvalidOperationException("SkipNotSupportedForSequenceTypes");
	    }

	    public static Exception CannotAssignToMember(string name)
	    {
			return new InvalidOperationException("CannotAssignToMember: " + name + ".");
	    }

	    public static Exception SkipRequiresSingleTableQueryWithPKs()
	    {
			return new InvalidOperationException("SkipRequiresSingleTableQueryWithPKs");
	    }

	    public static Exception ParameterNotInScope(string name)
	    {
			return new InvalidOperationException("ParameterNotInScope: " + name + ".");
	    }

	    public static Exception SprocsCannotBeComposed()
	    {
			return new InvalidOperationException("SprocsCannotBeComposed");
	    }

	    public static Exception InvalidReturnFromSproc(Type returnType)
	    {
			return new InvalidOperationException("InvalidReturnFromSproc: " + returnType + ".");
	    }

	    public static Exception UnhandledDeferredStorageType(Type type)
	    {
			return new InvalidOperationException("UnhandledDeferredStorageType: " + type + ".");
	    }

	    public static Exception TypeCouldNotBeAdded(Type type)
	    {
			return new InvalidOperationException("TypeCouldNotBeAdded: " + type + ".");
	    }

	    public static Exception ConvertToDateTimeOnlyForDateTimeOrString()
	    {
			return new InvalidOperationException("ConvertToDateTimeOnlyForDateTimeOrString");
	    }

	    public static Exception MismatchedThisKeyOtherKey(string name, string s)
	    {
			return new InvalidOperationException("MismatchedThisKeyOtherKey: " + name + ", " + s + ".");
	    }

	    public static Exception CantAddAlreadyExistingItem()
	    {
			return new InvalidOperationException("CantAddAlreadyExistingItem");
	    }

	    public static Exception DidNotExpectTypeChange(Type clrType, Type type)
	    {
			return new InvalidAsynchronousStateException("DidNotExpectTypeChange: " + clrType + ", " + type + ".");
	    }

	    public static Exception EntityIsTheWrongType()
	    {
			return new InvalidOperationException("EntityIsTheWrongType");
	    }

	    public static Exception UnexpectedNull(string metadatamember)
	    {
			return new InvalidOperationException("UnexpectedNull: " + metadatamember + ".");
	    }

	    public static Exception InvalidDeleteOnNullSpecification(object x)
	    {
			return new InvalidOperationException("InvalidDeleteOnNullSpecification: " + x + ".");
	    }

	    public static Exception ValueHasNoLiteralInSql(object value)
	    {
			return new InvalidAsynchronousStateException("ValueHasNoLiteralInSql: " + value + ".");
	    }

	    public static Exception CannotRemoveUnattachedEntity()
	    {
			return new InvalidAsynchronousStateException("CannotRemoveUnattachedEntity");
	    }

	    public static Exception CouldNotFindElementTypeInModel(string name)
	    {
			return new InvalidOperationException("CouldNotFindElementTypeInModel: " + name + ".");
	    }

	    public static Exception BinaryOperatorNotRecognized(ExpressionType nodeType)
	    {
			return new InvalidOperationException("BinaryOperatorNotRecognized: " + nodeType + ".");
	    }

	    public static Exception IncorrectNumberOfParametersMappedForMethod(string methodName)
	    {
			return new InvalidOperationException("IncorrectNumberOfParametersMappedForMethod: " + methodName + ".");
	    }

	    public static Exception TypeCouldNotBeTracked(Type type)
	    {
			return new InvalidOperationException("TypeCouldNotBeTracked: " + type + ".");
	    }

	    public static Exception ExpressionNotDeferredQuerySource()
	    {
			return new InvalidOperationException("ExpressionNotDeferredQuerySource");
	    }

	    public static Exception CannotAttachAsModifiedWithoutOriginalState()
	    {
			return new InvalidOperationException("CannotAttachAsModifiedWithoutOriginalState");
	    }

	    public static Exception IntersectNotSupportedForHierarchicalTypes()
	    {
			return new InvalidOperationException("IntersectNotSupportedForHierarchicalTypes");
	    }

	    public static Exception CannotAttachAlreadyExistingEntity()
	    {
			return new InvalidOperationException("CannotAttachAlreadyExistingEntity");
	    }

	    public static Exception ExceptNotSupportedForHierarchicalTypes()
	    {
			return new InvalidOperationException("ExceptNotSupportedForHierarchicalTypes");
	    }

	    public static Exception SequenceOperatorsNotSupportedForType(Type clrType)
	    {
			return new InvalidOperationException("SequenceOperatorsNotSupportedForType: " + clrType + ".");
	    }

	    public static Exception MemberAccessIllegal(MemberInfo member, Type reflectedType, Type clrType)
	    {
			return new InvalidOperationException("MemberAccessIllegal: " + member + ", " + reflectedType + ", " + clrType + ".");
	    }

	    public static Exception OriginalEntityIsWrongType()
	    {
			return new InvalidOperationException("OriginalEntityIsWrongType");
	    }

	    public static Exception QueryOnLocalCollectionNotSupported()
	    {
			return new InvalidOperationException("QueryOnLocalCollectionNotSupported");
	    }

	    public static Exception GroupingNotSupportedAsOrderCriterion()
	    {
			return new InvalidOperationException("GroupingNotSupportedAsOrderCriterion");
	    }

	    public static Exception TypeCannotBeOrdered(Type type)
	    {
			return new InvalidOperationException("TypeCannotBeOrdered: " + type + ".");
	    }

	    public static Exception BadKeyMember(string p0, string keyFields, string name)
	    {
			return new InvalidOperationException("BadKeyMember: " + p0 + ", " + keyFields + ", " + name + ".");
	    }

	    public static Exception NonConstantExpressionsNotSupportedFor(string stringContains)
	    {
			return new InvalidOperationException("NonConstantExpressionsNotSupportedFor: " + stringContains + ".");
	    }

	    public static Exception ColumnCannotReferToItself()
	    {
			return new InvalidOperationException("ColumnCannotReferToItself");
	    }

	    public static Exception IndexOfWithStringComparisonArgNotSupported()
	    {
			return new InvalidOperationException("IndexOfWithStringComparisonArgNotSupported");
	    }

	    public static Exception CannotPerformCUDOnReadOnlyTable(string toString)
	    {
			return new InvalidOperationException("CannotPerformCUDOnReadOnlyTable: " + toString + ".");
	    }

	    public static Exception CannotAggregateType(Type type)
	    {
			return new InvalidOperationException("CannotAggregateType: " + type + ".");
	    }

	    public static Exception CannotConvertToEntityRef(Type actualType)
	    {
			return new InvalidOperationException("CannotConvertToEntityRef: " + actualType + ".");
	    }

	    public static Exception NonCountAggregateFunctionsAreNotValidOnProjections(SqlNodeType aggType)
	    {
			return new InvalidOperationException("NonCountAggregateFunctionsAreNotValidOnProjections: " + aggType + ".");
	    }

	    public static Exception ArgumentTypeMismatch(string provider)
	    {
			return new InvalidOperationException("ArgumentTypeMismatch: " + provider + ".");
	    }

	    public static Exception CompiledQueryAgainstMultipleShapesNotSupported()
	    {
			return new InvalidOperationException("CompiledQueryAgainstMultipleShapesNotSupported");
	    }

	    public static Exception LastIndexOfWithStringComparisonArgNotSupported()
	    {
			return new InvalidOperationException("LastIndexOfWithStringComparisonArgNotSupported");
	    }

	    public static Exception GeneralCollectionMaterializationNotSupported()
	    {
			return new InvalidOperationException("GeneralCollectionMaterializationNotSupported");
	    }

	    public static Exception CannotEnumerateResultsMoreThanOnce()
	    {
			return new InvalidOperationException("CannotEnumerateResultsMoreThanOnce");
	    }

	    public static Exception MathRoundNotSupported()
	    {
			return new InvalidOperationException("MathRoundNotSupported");
	    }

	    public static Exception NonConstantExpressionsNotSupportedForRounding()
	    {
			return new InvalidOperationException("NonConstantExpressionsNotSupportedForRounding");
	    }

	    public static Exception QueryOperatorOverloadNotSupported(string name)
	    {
			return new InvalidOperationException("QueryOperatorOverloadNotSupported: " + name + ".");
	    }

	    public static Exception QueryOperatorNotSupported(string name)
	    {
			return new InvalidOperationException("QueryOperatorNotSupported: " + name + ".");
	    }

	    public static Exception InvalidSequenceOperatorCall(object x)
	    {
			return new InvalidOperationException("InvalidSequenceOperatorCall: " + x + ".");
	    }

	    public static Exception MemberCannotBeTranslated(Type declaringType, string name)
	    {
			return new InvalidOperationException("MemberCannotBeTranslated: " + declaringType + ", " + name + ".");
	    }

	    public static Exception InsertItemMustBeConstant()
	    {
			return new InvalidOperationException("InsertItemMustBeConstant");
	    }

	    public static Exception InvalidDbGeneratedType(string toQueryString)
	    {
			return new InvalidOperationException("InvalidDbGeneratedType: " + toQueryString + ".");
	    }

	    public static Exception UpdateItemMustBeConstant()
	    {
			return new InvalidOperationException("UpdateItemMustBeConstant");
	    }

	    public static Exception RequiredColumnDoesNotExist(string name)
	    {
			return new InvalidOperationException("RequiredColumnDoesNotExist: " + name + ".");
	    }

	    public static Exception NotSupported()
	    {
		    return new NotSupportedException();
	    }
    }
}
