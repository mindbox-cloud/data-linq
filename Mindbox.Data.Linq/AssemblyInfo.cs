using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

[assembly: InternalsVisibleTo("Mindbox.Data.Linq.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: SecurityTransparent]
[assembly: AllowPartiallyTrustedCallers]
[assembly: SecurityRules(SecurityRuleSet.Level1, SkipVerificationInFullTrust = true)]
[assembly: AssemblyTitle("Mindbox.Data.Linq")]
[assembly: AssemblyCompany("Mindbox")]
[assembly: AssemblyDescription("A clone of Microsoft System.Data.Linq to allow multi-DLL extensibility and EF compatibility.")]
[assembly: AssemblyVersion("2.0.1")]