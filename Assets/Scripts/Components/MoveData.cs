using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct MoveData : IComponentData
{
    public float MoveSpeed;
}