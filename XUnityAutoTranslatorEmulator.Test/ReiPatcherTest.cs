using Xunit;
using Xunit.Abstractions;
using XUnityAutoTranslatorEmulator.Frameworks;

namespace XUnityAutoTranslatorEmulator.Test
{
    public class ReiPatcherTest : AFrameworkTest
    {
        public ReiPatcherTest(ITestOutputHelper helper) : base("reipatcher-test", helper)
        {
        }

        [Fact]
        public void TestReiPatcher()
        {
            TestFramework(new ReiPatcher(GameFolder));
        }
    }
}
