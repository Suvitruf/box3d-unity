using NUnit.Framework;

namespace Box3d.Tests
{
    public class NativeLibraryTests
    {
        [Test]
        public void GetVersion_MatchesPinnedNativeBuild()
        {
            Box3dVersion version = Box3dApi.GetVersion();

            Assert.AreEqual(0, version.Major);
            Assert.AreEqual(1, version.Minor);
            Assert.AreEqual(0, version.Revision);
        }

        [Test]
        public void NativeBuild_IsSinglePrecision()
        {
            Assert.IsFalse(Box3dApi.IsDoublePrecision);
        }
    }
}
