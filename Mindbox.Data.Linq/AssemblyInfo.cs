using System.Runtime.CompilerServices;
using System.Security;

[assembly: InternalsVisibleTo("Mindbox.Data.Linq.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: SecurityTransparent]
[assembly: AllowPartiallyTrustedCallers]
[assembly: SecurityRules(SecurityRuleSet.Level1, SkipVerificationInFullTrust = true)]