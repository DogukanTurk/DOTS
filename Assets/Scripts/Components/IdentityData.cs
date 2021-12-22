using Unity.Entities;

[GenerateAuthoringComponent]
public struct IdentityData : IComponentData
{
    public int Team;

    public Entity Entity;
}