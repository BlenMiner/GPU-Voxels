namespace VoxelEngine
{
    internal interface IDirty
    {
        bool IsDirty {get;}

        void SetDirty();
        
        void ResetDirty();
    }
}