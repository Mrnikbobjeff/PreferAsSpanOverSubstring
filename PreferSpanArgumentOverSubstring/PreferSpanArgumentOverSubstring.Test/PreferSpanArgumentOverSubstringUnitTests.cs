using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using PreferSpanArgumentOverSubstring;

namespace PreferSpanArgumentOverSubstring.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        [TestMethod]
        public void NoDiagnostic_EmptyText()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void NoDiagnostic_StringPassed()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public int Test() => int.Parse("""");
        }
    }";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void NoDiagnostic_NoSpanOrStringOverload()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public void Accepts(object s) {}
            public int Test() => Accepts("""".Substring(1));
        }
    }";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void NoDiagnostic_NpParentInvocation()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public TypeName(string s) {}
            public string Test() => new TypeName("""".Substring(1));
        }
    }";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void Noiagnostic_StringPassedInsteadOfParameter()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public async Task<int> Test() => int.Parse("""".Substring(1));
        }
    }";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void SingleDiagnostic_StringPassedInsteadOfParameter()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public int Test() => int.Parse("""".Substring(1));
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "PreferSpanArgumentOverSubstring",
                Message = String.Format("Substring invocation '{0}' can be made more efficient", @""""".Substring(1)"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 8, 44)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void SingleFix_StringPassedInsteadOfParameter()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public int Test() => int.Parse("""".Substring(1));
        }
    }";
            var expected = @"using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public int Test() => int.Parse("""".AsSpan().Slice(1));
    }
}"; 
            VerifyCSharpFix(test, expected);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new PreferSpanArgumentOverSubstringCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new PreferSpanArgumentOverSubstringAnalyzer();
        }
    }
}
