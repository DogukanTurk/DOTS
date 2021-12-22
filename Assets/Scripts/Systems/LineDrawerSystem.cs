using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(DestroyingSystem))]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public class LineDrawerSystem : SystemBase
{

    /* ------------------------------------------ */

    protected override void OnUpdate()
    {
        Entities.ForEach((Entity entity, ref Translation translation, ref EnemyTargetData targetData) =>
        {
            Debug.DrawLine(translation.Value, targetData.Target.Position);
        }).Schedule();
    }

    /* ------------------------------------------ */

}
