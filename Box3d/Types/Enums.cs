namespace Box3d
{
    /// <summary>Mirrors native b3BodyType.</summary>
    public enum BodyType
    {
        Static = 0,
        Kinematic = 1,
        Dynamic = 2,
    }

    /// <summary>Mirrors native b3ShapeType (alphabetical in the engine).</summary>
    public enum ShapeType
    {
        Capsule = 0,
        Compound = 1,
        HeightField = 2,
        Hull = 3,
        Mesh = 4,
        Sphere = 5,
    }
}
