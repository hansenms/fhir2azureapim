using System;
using Xunit;
using fhir2apimlib;

namespace fhir2apimlib.Tests
{
    public class fhir2apimlibTests
    {
        [Fact]
        public void SimpleTestReturnsTrue()
        {
            Assert.True(Fhir2Apim.SimpleFunction());
        }
    }
}
