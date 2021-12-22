using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct GunData : IComponentData
{
    public int Damage;
    public int Range;

    [HideInInspector]
    public float NextFireTime;
}