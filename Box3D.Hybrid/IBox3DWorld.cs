using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Box3D.Hybrid
{

#nullable enable

    /// <summary>
    /// This delegate is used by the IBox3DWorld interface to provide an instance of itself anonymously. 
    /// Useful for game management logic in cases where you'd like to supply your own IBox3DWorld instance
    /// to internal systems, bypassing the lazy instantiation done by the Box3DWorld monobehaviour.
    /// This also allows you to juggle multiple world instances simultaneously, should that be required.
    /// </summary>
    public delegate IBox3DWorld? WorldProvider(Scene? scene, MonoBehaviour? requester);

    /// <summary>
    /// The Box3DWorld is responsible for tracking and updating Box3D bodies.
    /// Box3DWorld and Box3DEditorSimulation both provide a template implementation for IBox3DWorld
    /// </summary>
    public interface IBox3DWorld
    {

        public static bool Validate(IBox3DWorld? world)
        {
            if (world != null && world.IsValid) return true;
            Debug.LogError("An operation failed to proceed due to an invalidated IBox3DWorld");
            return false;
        }


        /// <summary>
        /// The provider supplies the IBox3DWorld instance to all internal systems.
        /// Note that this delegate is initially set to Box3DWorld's singleton
        /// getter, so the default reference returned by invoking Get() will 
        /// always be the singleton stored in Box3DWorld.
        /// </summary>
        [NoAutoStaticsCleanup]
        private static WorldProvider _provider = (scene, requester) => { return Box3DWorld.Instance; };

        /// <summary>
        /// Gets the IBox3DWorld instance for the given scene and requester.
        /// </summary>
        /// <returns></returns>
        public static IBox3DWorld? Get()
        {
            return _provider.Invoke(SceneManager.GetActiveScene(), null);
        }

        /// <summary>
        /// Gets the IBox3DWorld instance for the given scene and requester.
        /// </summary>
        /// <returns></returns>
        public static IBox3DWorld? Get(MonoBehaviour requester)
        {
            if (requester == null) return Get();
            return _provider.Invoke(requester.gameObject.scene, requester);
        }

        /// <summary>
        /// Gets the IBox3DWorld instance for the given scene and requester.
        /// </summary>
        /// <returns></returns>
        public static IBox3DWorld? Get(Scene scene, Box3DBody body)
        {
            return _provider.Invoke(scene, body);
        }

        /// <summary>
        /// Manually assigns the WorldProvider delegate. This delegate
        /// allows API users to specify a custom IBox3DWorld implementation
        /// to defer all operations towards. The default WorldProvider
        /// invokes the singleton getter located in the Box3DWorld monobehaviour.
        /// </summary>
        /// <returns></returns>
        public static void AssignProvider(WorldProvider? provider)
        {
            if (provider == null) _provider = (scene, requester) => { return Box3DWorld.Instance; };
            else _provider = provider;
        }

        [NoAutoStaticsCleanup]
        public static Vector3 DefaultGravity = new(0, -9.81f, 0);

        /// <summary>
        /// Gets the current gravity vector belonging to the scene. This method can be safely
        /// invoked outside of playmode for editor-time visualization.
        /// </summary>
        /// <returns></returns>
        public static Vector3 GetSceneGravity(MonoBehaviour? requester)
        {
#pragma warning disable CS8629
            return GetSceneGravityOrDefault(requester, DefaultGravity).Value;
#pragma warning restore CS8629
        }

        /// <summary>
        /// Gets the current gravity vector belonging to the scene. This method can be safely
        /// invoked outside of playmode for editor-time visualization.
        /// </summary>
        /// <returns></returns>
        public static Vector3? GetSceneGravityOrDefault(MonoBehaviour? requester, in Vector3? defaultGravity)
        {
            if (!Application.isPlaying) return DefaultGravity;
            IBox3DWorld? world = IBox3DWorld.Get(requester);
            return world == null || !world.IsValid ? defaultGravity : world.Gravity;
        }
#nullable disable


        /// <summary>The underlying Box3D world.</summary>
        public abstract Box3D.World PhysicsWorld { get; }

        /// <summary>
        /// The configured gravity vector 
        /// </summary>
        public abstract Vector3 Gravity { get; set; }

        /// <summary>
        /// A normalized, absolute vector pointing along the axis of gravity.
        /// </summary>
        public abstract Vector3 GravityDirection { get; }

        /// <summary>When true, the world stops stepping (bodies stay put). The visual replayer sets this
        /// so live physics doesn't fight the replayed transforms.</summary>
        public abstract bool IsPaused { get; set; }

        public abstract bool IsValid { get; }

        /// <summary>A shared static body at the origin, used as the fixed endpoint for joints whose
        /// connected body is null (like Unity's null connectedBody = attach to the world).</summary>
        public abstract Box3D.Body WorldAnchor { get; }

        /// <summary>
        /// To be called internally by bodies to add themselves to the world during initialization
        /// </summary>
        abstract void AddBody(Box3DBody body);

        /// <summary>
        /// To be called internally by bodies to remove themselves from the world during destruction
        /// </summary>
        abstract void RemoveBody(Box3DBody body);
    }
}