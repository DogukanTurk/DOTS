using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class EntitySpawnerSystem : SystemBase
{

    /* ------------------------------------------ */

    protected override void OnUpdate()
    {
        for (int x = 0; x < 2000; x++)
        {
            foreach (var entity in PrefabEntities.ShipEntities)
            {
                Entity tempEntity = EntityManager.Instantiate(entity);
                var _stats = GetComponent<IdentityData>(entity);
                EntityManager.SetComponentData(tempEntity, new MovingTargetData { RandomValue = Random.CreateFromIndex((uint)UnityEngine.Random.Range(1, 9999999999)) });

                Random random = Random.CreateFromIndex((uint)UnityEngine.Random.Range(1, 9999999999));

                if (_stats.Team == 0)
                {
                    EntityManager.SetComponentData(tempEntity, new Translation
                    {
                        Value = new float3(random.NextFloat(-75f, -25f), 0, random.NextFloat(-75f, -25f))
                    });
                }
                else
                {
                    EntityManager.SetComponentData(tempEntity, new Translation
                    {
                        Value = new float3(random.NextFloat(25f, 75f), 0, random.NextFloat(25f, 75f))
                    });
                }
            }
        }

        Enabled = false;
    }

    /* ------------------------------------------ */

}
