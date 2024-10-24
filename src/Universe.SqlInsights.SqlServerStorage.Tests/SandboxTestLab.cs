using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Universe.NUnitTests;

namespace Universe.SqlInsights.SqlServerStorage.Tests
{
    public class SandboxTestLab : NUnitTestsBase
    {
        [Test]
        [Category("Skip"), Explicit]
        public void TestThrowException()
        {
            throw new ApplicationException("Exception on purpose");
        }

    }
}
