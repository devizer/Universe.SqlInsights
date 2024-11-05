using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Library
{
    // TODO: Remove it
    public class DbTestCaseSource : TestCaseSourceAttribute
    {
        protected DbTestCaseSource(string sourceName) : base(sourceName)
        {
        }
    }
}
