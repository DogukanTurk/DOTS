using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(QuadrantSystem))]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public class FindingTargetSystem : SystemBase
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

        EntityCommandBuffer.ParallelWriter ecb = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        // Clearing the Enemy Target Data
        var idnEnemyTargetDataQuery = GetEntityQuery(typeof(IdentityData), typeof(EnemyTargetData));
        var idData = GetComponentTypeHandle<IdentityData>(true);

        var removingTagJob = new RemovingTagJob
        {
            entityCommandBuffer = ecb,

            IdentityDataHandler = idData,
        };
        Dependency = removingTagJob.ScheduleParallel(idnEnemyTargetDataQuery, 1, Dependency);

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        Dependency.Complete();


        // Finding closest target
        var generalQuery = GetEntityQuery(typeof(Translation), ComponentType.ReadOnly<IdentityData>(), ComponentType.ReadOnly<GunData>(), ComponentType.Exclude<EnemyTargetData>());

        var translationType = GetComponentTypeHandle<Translation>(true);
        var identityDataType = GetComponentTypeHandle<IdentityData>(true);
        var gunDataType = GetComponentTypeHandle<GunData>(true);

        NativeQueue<EnemyTargetData> targets = new NativeQueue<EnemyTargetData>(Allocator.TempJob);

        var findingJob = new FindingTargetJob()
        {
            QuadrantMultiHashMap = QuadrantSystem.QuadrantMultiHashMap,

            TranslationHandler = translationType,
            IdentityDataHandler = identityDataType,
            GunDataHandler = gunDataType,

            Targets = targets.AsParallelWriter()
        };
        Dependency = findingJob.ScheduleParallel(generalQuery, 1, Dependency);
        Dependency.Complete();


        // Adding Enemy Target Data to attacker
        if (targets.Count > 0)
        {
            var addingTagJob = new AddingTagJob
            {
                entityCommandBuffer = ecb,

                IdentityDataHandler = identityDataType,
                TargetIdentityDataHandler = targets
            };
            Dependency = addingTagJob.ScheduleParallel(generalQuery, 1, Dependency);

            endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
            Dependency.Complete();
        }


        // Clearing targets native array
        targets.Dispose();
    }

    /* ------------------------------------------ */

    [BurstCompile]
    private struct FindingTargetJob : IJobEntityBatch
    {

        /* ------------------------------------------ */

        public NativeQueue<EnemyTargetData>.ParallelWriter Targets;

        [ReadOnly] public ComponentTypeHandle<Translation> TranslationHandler;
        [ReadOnly] public ComponentTypeHandle<IdentityData> IdentityDataHandler;
        [ReadOnly] public ComponentTypeHandle<GunData> GunDataHandler;

        [ReadOnly] public NativeMultiHashMap<int, QuadrantData> QuadrantMultiHashMap;

        /* ------------------------------------------ */

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var chunkTranslations = batchInChunk.GetNativeArray(TranslationHandler);
            var chunkIdentityDatas = batchInChunk.GetNativeArray(IdentityDataHandler);
            var chunkGunDatas = batchInChunk.GetNativeArray(GunDataHandler);

            for (var i = 0; i < batchInChunk.Count; i++)
            {
                var translation = chunkTranslations[i];
                var identityData = chunkIdentityDatas[i];
                var gunData = chunkGunDatas[i];

                float targetEntityDistance = float.MaxValue;
                float tmpDistance = 0f;

                int hashMapKey = QuadrantSystem.GetPositionHashMapKey(translation.Value);

                QuadrantData quadrantData;
                NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;


                if (QuadrantMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator))
                {
                    EnemyTargetData targetEntity = new EnemyTargetData();

                    do
                    {
                        if (identityData.Team != quadrantData.Identity.Team && identityData.Entity != quadrantData.Identity.Entity)
                        {
                            tmpDistance = math.distance(translation.Value, quadrantData.Position);

                            if (tmpDistance <= gunData.Range)
                            {
                                if (targetEntity.Target.Identity.Entity == Entity.Null)
                                    targetEntity.Target = quadrantData;
                                else
                                {
                                    if (targetEntityDistance > tmpDistance)
                                        targetEntity.Target = quadrantData;
                                }
                            }
                        }
                    }
                    while (QuadrantMultiHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));


                    //If target found set it to the array
                    if (targetEntity.Target.Identity.Entity != Entity.Null)
                    {
                        targetEntity.Attacker = identityData;
                        Targets.Enqueue(targetEntity);
                    }
                }

            }
        }

        /* ------------------------------------------ */

    }

    [BurstCompile]
    private struct AddingTagJob : IJobEntityBatch
    {

        /* ------------------------------------------ */

        public EntityCommandBuffer.ParallelWriter entityCommandBuffer;

        [ReadOnly] public NativeQueue<EnemyTargetData> TargetIdentityDataHandler;
        [ReadOnly] public ComponentTypeHandle<IdentityData> IdentityDataHandler;

        /* ------------------------------------------ */

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var chunkIdentityDatas = batchInChunk.GetNativeArray(IdentityDataHandler);
            var chunkTargetIdentityDatas = TargetIdentityDataHandler.ToArray(Allocator.Temp);

            for (var i = 0; i < chunkTargetIdentityDatas.Length; i++)
            {
                for (int x = 0; x < chunkIdentityDatas.Length; x++)
                {
                    if (chunkIdentityDatas[x].Entity == chunkTargetIdentityDatas[i].Attacker.Entity)
                        entityCommandBuffer.AddComponent(x, chunkIdentityDatas[x].Entity, new EnemyTargetData { Target = chunkTargetIdentityDatas[i].Target, Attacker = chunkTargetIdentityDatas[i].Attacker });
                }
            }
        }

        /* ------------------------------------------ */

    }

    [BurstCompile]
    private struct RemovingTagJob : IJobEntityBatch
    {

        /* ------------------------------------------ */

        public EntityCommandBuffer.ParallelWriter entityCommandBuffer;

        [ReadOnly] public ComponentTypeHandle<IdentityData> IdentityDataHandler;

        /* ------------------------------------------ */

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var chunkIdentityDatas = batchInChunk.GetNativeArray(IdentityDataHandler);

            for (var i = 0; i < batchInChunk.Count; i++)
                entityCommandBuffer.RemoveComponent<EnemyTargetData>(i, chunkIdentityDatas[i].Entity);
        }

        /* ------------------------------------------ */

    }


    /* ------------------------------------------ */

}






























































































//NativeArray<QuadrantData> tmpQuadrantDataArray = QuadrantMultiHashMap.GetValueArray(Allocator.Temp);

//foreach (var quadrantData in tmpQuadrantDataArray)
//{
//    if (identityData.Team != quadrantData.Identity.Team && identityData.Entity != quadrantData.Identity.Entity)
//    {
//        tmpDistance = math.distance(translation.Value, quadrantData.Position);

//        if (tmpDistance <= gunData.Range)
//        {
//            if (targetEntity == Entity.Null)
//            {
//                targetEntity = quadrantData.Identity.Entity;
//                tmpIndexSave = tmpIndex;

//                targetEntityDistance = tmpDistance;
//            }
//            else
//            {
//                if (targetEntityDistance > tmpDistance)
//                {
//                    targetEntity = quadrantData.Identity.Entity;
//                    tmpIndexSave = tmpIndex;

//                    targetEntityDistance = tmpDistance;
//                }
//            }
//        }
//    }

//    tmpIndex++;
//}


////If target found set it to the array
//if (targetEntity != Entity.Null && tmpIndexSave > -1)
//    Targets.Enqueue(tmpQuadrantDataArray[tmpIndexSave].Identity);