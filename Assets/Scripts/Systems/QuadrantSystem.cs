using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class QuadrantSystem : SystemBase
{

    /* ------------------------------------------ */

    public static NativeMultiHashMap<int, QuadrantData> QuadrantMultiHashMap;

    /* ------------------------------------------ */

    private const int _quadrantYMultiplier = 1000;
    private const int _quadrantCellSize = 25;

    /* ------------------------------------------ */

    public static int GetPositionHashMapKey(float3 position)
    {
        return (int)(math.floor(position.x / _quadrantCellSize) + (_quadrantYMultiplier * math.floor(position.z / _quadrantCellSize)));
    }

    /* ------------------------------------------ */

    protected override void OnCreate()
    {
        QuadrantMultiHashMap = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
    }

    protected override void OnUpdate()
    {
        EntityQuery entityQuery = GetEntityQuery(typeof(Translation), typeof(IdentityData));

        QuadrantMultiHashMap.Clear();

        if (entityQuery.CalculateEntityCount() > QuadrantMultiHashMap.Capacity)
            QuadrantMultiHashMap.Capacity = entityQuery.CalculateEntityCount();

        var translationType = GetComponentTypeHandle<Translation>(true);
        var identityDataType = GetComponentTypeHandle<IdentityData>(true);

        SetQuadrantDataHashMapJob job = new SetQuadrantDataHashMapJob
        {
            QuadrantMultiHashMap = QuadrantMultiHashMap.AsParallelWriter(),

            TranslationHandler = translationType,
            IdentityDataHandler = identityDataType
        };
        Dependency = job.ScheduleParallel(entityQuery, 1, Dependency);

        Dependency.Complete();
    }

    protected override void OnDestroy()
    {
        QuadrantMultiHashMap.Dispose();
    }

    /* ------------------------------------------ */

    [BurstCompile]
    struct SetQuadrantDataHashMapJob : IJobEntityBatch
    {

        /* ------------------------------------------ */

        public NativeMultiHashMap<int, QuadrantData>.ParallelWriter QuadrantMultiHashMap;

        [ReadOnly] public ComponentTypeHandle<Translation> TranslationHandler;
        [ReadOnly] public ComponentTypeHandle<IdentityData> IdentityDataHandler;

        /* ------------------------------------------ */

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var chunkTranslations = batchInChunk.GetNativeArray(TranslationHandler);
            var chunkIdentityDatas = batchInChunk.GetNativeArray(IdentityDataHandler);

            for (var i = 0; i < batchInChunk.Count; i++)
            {
                var translation = chunkTranslations[i];
                var identityData = chunkIdentityDatas[i];

                int hashMapKey = GetPositionHashMapKey(translation.Value);
                QuadrantMultiHashMap.Add(hashMapKey, new QuadrantData { Identity = identityData, Position = translation.Value });
            }
        }

        /* ------------------------------------------ */
    }

    /* ------------------------------------------ */

}
