using NUnit.Framework;

namespace Box3D.Tests
{
    public class NativeLibraryTests
    {
        [Test]
        public void GetVersion_MatchesPinnedNativeBuild()
        {
            // Update these on box3d version bumps (see Box3D.Native~/VERSION).
            Box3DVersion version = Box3DApi.GetVersion();

            Assert.AreEqual(0, version.Major);
            Assert.AreEqual(1, version.Minor);
            Assert.AreEqual(0, version.Revision);
        }

        [Test]
        public void NativeBuild_PrecisionMatchesManagedDefine()
        {
            // A mismatch means every position-carrying struct has the wrong layout — the same
            // condition Box3DRuntime guards at init, asserted here so CI fails loudly.
#if BOX3D_DOUBLE
            Assert.IsTrue(Box3DApi.IsDoublePrecision, "BOX3D_DOUBLE is defined — the loaded native library must be a double-precision build");
#else
            Assert.IsFalse(Box3DApi.IsDoublePrecision, "BOX3D_DOUBLE is not defined — the loaded native library must be a single-precision build");
#endif
        }
    }
}
