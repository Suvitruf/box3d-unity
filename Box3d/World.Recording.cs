namespace Box3d
{
    public partial struct World
    {
        /// <summary>Begins capturing this world's mutations into <paramref name="recording"/>. Stop with
        /// <see cref="StopRecording"/>, then validate determinism, save, or replay it. Record from the
        /// world's initial state (right after creation) so the whole run is reproducible.</summary>
        public unsafe void StartRecording(Recording recording)
        {
            UnsafeBindings.b3World_StartRecording(Id, (b3Recording*)recording.Handle);
        }

        // StopRecording() is generated (World.g.cs).
    }
}
