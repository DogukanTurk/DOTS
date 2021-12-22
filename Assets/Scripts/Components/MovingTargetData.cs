using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct MovingTargetData : IComponentData
{
    public float3 TargetPosition;

    public Random RandomValue;
}