using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class MovementSystem : SystemBase
{

    /* ------------------------------------------ */

    EntityQuery m_Query;

    /* ------------------------------------------ */

    protected override void OnCreate()
    {
        m_Query = GetEntityQuery(typeof(Translation), typeof(MovingTargetData), ComponentType.ReadOnly<MoveData>());
    }

    protected override void OnUpdate()
    {
        var translationType = GetComponentTypeHandle<Translation>();
        var targetDataType = GetComponentTypeHandle<MovingTargetData>();
        var moveDataType = GetComponentTypeHandle<MoveData>(true);

        var job = new MovementJob()
        {
            DeltaTime = Time.DeltaTime,

            TranslationHandle = translationType,
            TargetDataHandle = targetDataType,
            MoveDataHandle = moveDataType
        };
        Dependency = job.ScheduleParallel(m_Query, 1, Dependency);
        Dependency.Complete();
    }

    /* ------------------------------------------ */

    [BurstCompile]
    struct MovementJob : IJobEntityBatch
    {

        /* ------------------------------------------ */
        // Variables

        public float DeltaTime;

        /* ------------------------------------------ */
        // Handlers

        public ComponentTypeHandle<Translation> TranslationHandle;
        public ComponentTypeHandle<MovingTargetData> TargetDataHandle;

        [ReadOnly]
        public ComponentTypeHandle<MoveData> MoveDataHandle;

        /* ------------------------------------------ */
        // Functions

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var chunkTranslations = batchInChunk.GetNativeArray(TranslationHandle);
            var chunkMoveDatas = batchInChunk.GetNativeArray(MoveDataHandle);
            var chunkTargetDatas = batchInChunk.GetNativeArray(TargetDataHandle);

            for (var i = 0; i < batchInChunk.Count; i++)
            {
                var translation = chunkTranslations[i];
                var moveData = chunkMoveDatas[i];
                var targetData = chunkTargetDatas[i];


                // If the Random isn't properly initialized, init
                if (targetData.RandomValue.state == 0)
                    targetData.RandomValue = Unity.Mathematics.Random.CreateFromIndex((uint)i);

                // If there is no target, get me a target.
                if (targetData.TargetPosition.Equals(float3.zero))
                    targetData.TargetPosition = new float3(translation.Value.x + targetData.RandomValue.NextFloat(-50f, 50f), 0, translation.Value.z + targetData.RandomValue.NextFloat(-50f, 50f));

                // If I'm not there yet, keep moving
                if (math.distance(translation.Value, targetData.TargetPosition) > .1f)
                    translation.Value += math.normalize(targetData.TargetPosition - translation.Value) * moveData.MoveSpeed * DeltaTime;
                else // If I'm already there or passed there, get me a new target
                    targetData.TargetPosition = new float3(translation.Value.x + targetData.RandomValue.NextFloat(-50f, 50f), 0, translation.Value.z + targetData.RandomValue.NextFloat(-50f, 50f));


                // Set the values back
                chunkTranslations[i] = translation;
                chunkTargetDatas[i] = targetData;
            }
        }

        /* ------------------------------------------ */

    }

    /* ------------------------------------------ */

}
