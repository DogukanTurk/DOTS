using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct EnemyTargetData : IComponentData
{
    public IdentityData Attacker;

    public QuadrantData Target;
}