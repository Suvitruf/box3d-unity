namespace Box3D
{
    /// <summary>One proxy returned by a <see cref="DynamicTree"/> query: its id and the
    /// <c>userData</c> you attached at <see cref="DynamicTree.CreateProxy"/>.</summary>
    public readonly struct DynamicTreeHit
    {
        public readonly int ProxyId;
        public readonly ulong UserData;

        public DynamicTreeHit(int proxyId, ulong userData)
        {
            ProxyId = proxyId;
            UserData = userData;
        }
    }
}
