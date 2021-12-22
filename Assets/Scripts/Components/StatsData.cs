using Unity.Entities;

[GenerateAuthoringComponent]
public struct StatsData : IComponentData
{
    public IdentityData Identity;

    public int Health;
}