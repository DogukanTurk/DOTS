using Unity.Entities;
using Unity.Mathematics;

public struct QuadrantData : IComponentData
{
    public IdentityData Identity;

    public float3 Position;
}