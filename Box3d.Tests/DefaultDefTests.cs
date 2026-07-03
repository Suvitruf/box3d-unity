using NUnit.Framework;

namespace Box3d.Tests
{
    /// <summary>Validates the b3Default…() factories round-trip correctly through the managed
    /// mirrors. Because fields sit at many different offsets (including trailing bools after
    /// pointers), correct values here are strong evidence the struct layouts match the native ABI.
    /// Expected values come from box3d src/types.c.</summary>
    public class DefaultDefTests
    {
        [Test]
        public void WorldDef_DefaultsMatchNative()
        {
            WorldDef def = WorldDef.Default;

            Assert.AreEqual(0f, def.Gravity.x);
            Assert.AreEqual(-10f, def.Gravity.y);
            Assert.AreEqual(0f, def.Gravity.z);
            Assert.AreEqual(1f, def.RestitutionThreshold);
            Assert.AreEqual(1f, def.HitEventThreshold);
            Assert.AreEqual(30f, def.ContactHertz);
            Assert.AreEqual(10f, def.ContactDampingRatio);
            Assert.AreEqual(3f, def.ContactSpeed);
            Assert.AreEqual(400f, def.MaximumLinearSpeed);
            Assert.IsTrue(def.EnableSleep);
            Assert.IsTrue(def.EnableContinuous);
            Assert.AreNotEqual(0, def.InternalValue, "internalValue cookie missing — layout is wrong");
        }

        [Test]
        public void BodyDef_DefaultsMatchNative()
        {
            BodyDef def = BodyDef.Default;

            Assert.AreEqual(BodyType.Static, def.Type);
            Assert.AreEqual(0f, def.Rotation.value.x);
            Assert.AreEqual(1f, def.Rotation.value.w, "rotation should be identity");
            Assert.AreEqual(1f, def.GravityScale);
            Assert.AreEqual(0.05f, def.SleepThreshold, 1e-6f);
            Assert.IsTrue(def.EnableSleep);
            Assert.IsTrue(def.IsAwake);
            Assert.IsFalse(def.IsBullet);
            Assert.IsTrue(def.IsEnabled);
            Assert.IsFalse(def.AllowFastRotation);
            Assert.IsTrue(def.EnableContactRecycling);
            Assert.AreNotEqual(0, def.InternalValue, "internalValue cookie missing — layout is wrong");
        }

        [Test]
        public void ShapeDef_DefaultsMatchNative()
        {
            ShapeDef def = ShapeDef.Default;

            Assert.AreEqual(1000f, def.Density);
            Assert.AreEqual(1f, def.ExplosionScale);
            Assert.IsFalse(def.IsSensor);
            Assert.IsFalse(def.EnableContactEvents);
            Assert.IsTrue(def.InvokeContactCreation);
            Assert.IsTrue(def.UpdateBodyMass);
            Assert.AreNotEqual(0, def.InternalValue, "internalValue cookie missing — layout is wrong");
        }
    }
}
