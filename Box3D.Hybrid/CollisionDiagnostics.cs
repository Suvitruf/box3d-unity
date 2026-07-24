using System.Collections.Generic;

namespace Box3D.Hybrid
{
    /// <summary>Outcome of one diagnostic check.</summary>
    public enum DiagnosisStatus { Pass, Fail, Note }

    /// <summary>One line of a collision diagnosis.</summary>
    public struct DiagnosisLine
    {
        public DiagnosisStatus Status;
        public string Label;
        public string Detail;
    }

    /// <summary>Explains whether two bodies can collide, and if not, why — mirroring box3d's own rules
    /// (<c>b3ShouldShapesCollide</c> filter test + <c>b3ShouldBodiesCollide</c> body/joint test), plus the
    /// sensor and broadphase-AABB context. The "why isn't this colliding?" answer.</summary>
    public static class CollisionDiagnostics
    {
        /// <summary>Runs the checks on two live bodies. <paramref name="canCollide"/> is true only if the
        /// hard rules (types, enabled, joints, filter) all pass; sensor / AABB proximity are reported as
        /// notes. <paramref name="summary"/> is a one-line verdict.</summary>
        public static List<DiagnosisLine> Diagnose(Body a, Body b, out bool canCollide, out string summary)
        {
            var lines = new List<DiagnosisLine>();
            bool collide = true;
            string firstFail = null;

            void Fail(string label, string detail)
            {
                lines.Add(new DiagnosisLine { Status = DiagnosisStatus.Fail, Label = label, Detail = detail });
                collide = false;
                firstFail ??= label + " — " + detail;
            }
            void Pass(string label, string detail) =>
                lines.Add(new DiagnosisLine { Status = DiagnosisStatus.Pass, Label = label, Detail = detail });
            void Note(string label, string detail) =>
                lines.Add(new DiagnosisLine { Status = DiagnosisStatus.Note, Label = label, Detail = detail });

            if (a.Equals(b))
            {
                Fail("Distinct bodies", "Body A and Body B are the same body.");
                canCollide = false;
                summary = "Same body — a body doesn't collide with itself.";
                return lines;
            }

            // Body types: two non-dynamic bodies never collide (b3ShouldBodiesCollide).
            bool oneDynamic = a.Type == BodyType.Dynamic || b.Type == BodyType.Dynamic;
            if (oneDynamic)
                Pass("At least one body is dynamic", $"A: {a.Type}, B: {b.Type}");
            else
                Fail("At least one body is dynamic", $"Both are non-dynamic (A: {a.Type}, B: {b.Type}) — static/kinematic pairs never collide.");

            // Enabled.
            if (a.IsEnabled && b.IsEnabled)
                Pass("Both bodies enabled", "");
            else
                Fail("Both bodies enabled", $"A enabled: {a.IsEnabled}, B enabled: {b.IsEnabled} — disabled bodies don't collide.");

            // Joints with CollideConnected off.
            if (TryFindBlockingJoint(a, b, out string jointDesc))
                Fail("No joint disables their collision", jointDesc);
            else
                Pass("No joint disables their collision", "");

            // Shape-level: filter, sensor, AABB proximity.
            Shape[] shapesA = GetShapes(a);
            Shape[] shapesB = GetShapes(b);
            if (shapesA.Length == 0 || shapesB.Length == 0)
            {
                Fail("Both bodies have shapes", $"A has {shapesA.Length} shape(s), B has {shapesB.Length}.");
            }
            else
            {
                DiagnoseShapePairs(shapesA, shapesB, Pass, Fail, Note);
            }

            canCollide = collide;
            summary = collide
                ? "✔ These bodies CAN collide."
                : "✘ These bodies will NOT collide: " + firstFail;
            return lines;
        }

        private static void DiagnoseShapePairs(Shape[] shapesA, Shape[] shapesB,
            System.Action<string, string> pass, System.Action<string, string> fail, System.Action<string, string> note)
        {
            bool anyFilterPass = false;
            string firstFilterReason = null;
            bool anyAabbOverlap = false;
            bool passingPairIsSensor = false;

            foreach (Shape sa in shapesA)
            {
                foreach (Shape sb in shapesB)
                {
                    bool collides = ShouldShapesCollide(sa.GetFilter(), sb.GetFilter(), out string reason);
                    firstFilterReason ??= reason;
                    if (collides)
                    {
                        anyFilterPass = true;
                        if (sa.IsSensor() || sb.IsSensor()) passingPairIsSensor = true;
                    }
                    if (AabbOverlap(sa.GetAABB(), sb.GetAABB())) anyAabbOverlap = true;
                }
            }

            if (anyFilterPass)
                pass("Collision filters match", "At least one shape pair passes category/mask/group.");
            else
                fail("Collision filters match", firstFilterReason ?? "no shape pair passes the filter.");

            if (passingPairIsSensor)
                note("Sensor", "A matching shape is a sensor — it reports overlap via events, not a solid contact.");

            if (!anyAabbOverlap)
                note("Currently near each other", "Their broadphase AABBs don't overlap right now — they're not close enough to touch (position-dependent; move them together to test).");
        }

        // Mirrors b3ShouldShapesCollide.
        private static bool ShouldShapesCollide(CollisionFilter fa, CollisionFilter fb, out string reason)
        {
            if (fa.GroupIndex == fb.GroupIndex && fa.GroupIndex != 0)
            {
                bool same = fa.GroupIndex > 0;
                reason = same
                    ? $"same group index {fa.GroupIndex} (>0) forces collision"
                    : $"same group index {fa.GroupIndex} (<0) forces NEVER-collide — this overrides categories/masks";
                return same;
            }

            bool aSeesB = (fa.MaskBits & fb.CategoryBits) != 0;
            bool bSeesA = (fb.MaskBits & fa.CategoryBits) != 0;
            if (aSeesB && bSeesA)
            {
                reason = "categories and masks match both ways";
                return true;
            }
            if (!aSeesB && !bSeesA)
                reason = $"neither mask includes the other's category (A cat 0x{fa.CategoryBits:X}/mask 0x{fa.MaskBits:X}, B cat 0x{fb.CategoryBits:X}/mask 0x{fb.MaskBits:X})";
            else if (!aSeesB)
                reason = $"A's mask (0x{fa.MaskBits:X}) excludes B's category (0x{fb.CategoryBits:X})";
            else
                reason = $"B's mask (0x{fb.MaskBits:X}) excludes A's category (0x{fa.CategoryBits:X})";
            return false;
        }

        private static bool TryFindBlockingJoint(Body a, Body b, out string description)
        {
            description = null;
            int count = a.GetJointCount();
            if (count == 0) return false;

            var ids = new JointId[count];
            int written = a.GetJoints(ids);
            for (int i = 0; i < written; i++)
            {
                var joint = new Joint { Id = ids[i] };
                if (!joint.IsValid || joint.CollideConnected) continue;
                if (joint.BodyA.Equals(b) || joint.BodyB.Equals(b))
                {
                    description = "A joint between them has Collide Connected turned off.";
                    return true;
                }
            }
            return false;
        }

        private static Shape[] GetShapes(Body body)
        {
            int count = body.GetShapeCount();
            if (count == 0) return System.Array.Empty<Shape>();
            var ids = new ShapeId[count];
            int written = body.GetShapes(ids);
            var shapes = new Shape[written];
            for (int i = 0; i < written; i++) shapes[i] = new Shape { Id = ids[i] };
            return shapes;
        }

        private static bool AabbOverlap(B3Aabb x, B3Aabb y)
        {
            return x.LowerBound.x <= y.UpperBound.x && x.UpperBound.x >= y.LowerBound.x
                && x.LowerBound.y <= y.UpperBound.y && x.UpperBound.y >= y.LowerBound.y
                && x.LowerBound.z <= y.UpperBound.z && x.UpperBound.z >= y.LowerBound.z;
        }
    }
}
