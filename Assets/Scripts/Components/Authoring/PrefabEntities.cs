using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class PrefabEntities : MonoBehaviour, IConvertGameObjectToEntity
{

    /* ------------------------------------------ */

    public static List<Entity> ShipEntities = new();
    public static Entity ExplosionEntity;

    public GameObject[] Ships;
    public GameObject ExplosionPrefab;

    /* ------------------------------------------ */

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        foreach (var obj in Ships)
        {
            using (BlobAssetStore blobAssetStore = new BlobAssetStore())
            {
                Entity prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(obj,
                GameObjectConversionSettings.FromWorld(dstManager.World, blobAssetStore));

                ShipEntities.Add(prefabEntity);
            }
        }

        using (BlobAssetStore blobAssetStore = new BlobAssetStore())
        {
            Entity prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(ExplosionPrefab,
            GameObjectConversionSettings.FromWorld(dstManager.World, blobAssetStore));

            ExplosionEntity = prefabEntity;
        }
    }
    
    /* ------------------------------------------ */

}
