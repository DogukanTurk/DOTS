using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(AttackingSystem))]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public class DestroyingSystem : SystemBase
{

    /* ------------------------------------------ */

    BeginInitializationEntityCommandBufferSystem ecbSystem;

    protected override void OnCreate()
    {
        ecbSystem = World.GetExistingSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer.ParallelWriter ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
        var explosionPrefab = PrefabEntities.ExplosionEntity;

        Entities.WithChangeFilter<StatsData>().ForEach((Entity entity, int entityInQueryIndex,in StatsData statsData, in LocalToWorld ltw) =>{
            if (statsData.Health <= 0f)
            {
                ecb.DestroyEntity(entityInQueryIndex, entity);

                var explosion = ecb.Instantiate(entityInQueryIndex, explosionPrefab);
                ecb.SetComponent(entityInQueryIndex, explosion, new Translation { Value = ltw.Position });
            }
        }).ScheduleParallel();

        ecbSystem.AddJobHandleForProducer(Dependency);
    }

    /* ------------------------------------------ */

}
