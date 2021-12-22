using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class AttackingSystem : SystemBase
{

    /* ------------------------------------------ */

    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    /* ------------------------------------------ */

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityQuery attackingQuery = GetEntityQuery(typeof(GunData), ComponentType.ReadOnly<EnemyTargetData>());
        var gunDataType = GetComponentTypeHandle<GunData>();
        var enemyTargetDataType = GetComponentTypeHandle<EnemyTargetData>(true);

        var attackingJob = new AttackingJob()
        {
            entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter(),

            Time = (float)Time.ElapsedTime,

            StatsData = GetComponentDataFromEntity<StatsData>(),
            GunData = gunDataType,
            EnemyTargetData = enemyTargetDataType
        };
        Dependency = attackingJob.ScheduleParallel(attackingQuery, 1, Dependency);
        Dependency.Complete();
    }

    /* ------------------------------------------ */

    [BurstCompile]
    struct AttackingJob : IJobEntityBatch
    {

        /* ------------------------------------------ */

        public float Time;

        /* ------------------------------------------ */

        public EntityCommandBuffer.ParallelWriter entityCommandBuffer;

        [NativeDisableContainerSafetyRestriction] public ComponentDataFromEntity<StatsData> StatsData;

        public ComponentTypeHandle<GunData> GunData;
        [ReadOnly] public ComponentTypeHandle<EnemyTargetData> EnemyTargetData;

        /* ------------------------------------------ */

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var chunkGunDatas = batchInChunk.GetNativeArray(GunData);
            var chunkEnemyTargetDatas = batchInChunk.GetNativeArray(EnemyTargetData);

            for (var i = 0; i < chunkEnemyTargetDatas.Length; i++)
            {
                var gunData = chunkGunDatas[i];
                var enemyTargetData = chunkEnemyTargetDatas[i];


                if (StatsData.HasComponent(enemyTargetData.Target.Identity.Entity))
                {
                    StatsData statData = StatsData[enemyTargetData.Target.Identity.Entity];

                    // Every 3/4 second, it can fire
                    if (Time > gunData.NextFireTime)
                    {

                        gunData.NextFireTime = Time + .75f;

                        statData.Health -= gunData.Damage;

                        if (enemyTargetData.Target.Identity.Entity != Entity.Null)
                            entityCommandBuffer.SetComponent(i, enemyTargetData.Target.Identity.Entity, new StatsData { Health = statData.Health, Identity = statData.Identity });

                        if (statData.Health <= 0)
                            if (enemyTargetData.Attacker.Entity != Entity.Null)
                                entityCommandBuffer.RemoveComponent<EnemyTargetData>(i, enemyTargetData.Attacker.Entity);

                        chunkGunDatas[i] = gunData;
                    }
                }

            }

        }

        /* ------------------------------------------ */

    }

    /* ------------------------------------------ */

}
