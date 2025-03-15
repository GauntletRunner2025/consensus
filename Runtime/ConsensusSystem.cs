using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public abstract partial class ConsensusSystem : SystemBase
{
    //This marks which entities we are interested in analyzing
    public abstract EntityQuery InterestingEntitiesSignature { get; }

    //This gets put on entities we are interested in
    //so we can find them later
    //Also the base SystemFlag type (an IComponentData) distinguishes our blocker from other systems' blockers
    public abstract ComponentType SystemFlag { get; }

    public abstract void AnalyzeEntity(EntityManager em, Entity entity, out bool isComplete);
    public EntityQuery EntitiesWithSystemFlagAndBlockerBuffer;
    public abstract void SetupQuery(EntityManager em);

    sealed protected override void OnCreate()
    {
        SetupQuery(EntityManager);

        var entityQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] { SystemFlag, typeof(ConsensusBlocker) },
            Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
        };
        EntitiesWithSystemFlagAndBlockerBuffer = EntityManager.CreateEntityQuery(entityQueryDesc);

        // Debug.Log($"[{this.GetType().Name}] Created Consensus Interest component");
        var e = EntityManager.CreateEntity();
        EntityManager.AddComponentData(e, new ConsensusInterest
        {
            Query = InterestingEntitiesSignature,
            SystemFlag = SystemFlag
        });
    }

    sealed protected override void OnStartRunning()
    {

    }

    sealed protected override void OnUpdate()
    {
        using EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        using var entities = EntitiesWithSystemFlagAndBlockerBuffer.ToEntityArray(Allocator.TempJob);
        // Debug.Log($"[{this.GetType().Name}] Found {entities.Length} entities with {SystemFlag} and ConsensusBlocker");

        foreach (var entity in entities)
        {
            AnalyzeEntity(this.EntityManager, entity, out bool shouldRemoveBlocker);

            if (!shouldRemoveBlocker)
            {
                Debug.Log($"[{this.GetType().Name}] {entity} is not ready to remove {SystemFlag}");
                continue;
            }

            EntityManager.RemoveComponent(entity, SystemFlag);

            // Debug.Log($"Removed {SystemFlag} from {entity}");

            //Get the dynamic buffer of blockers
            var item = EntityManager.GetBuffer<ConsensusBlocker>(entity);

            //Remove the blocker
            for (int i = 0; i < item.Length; i++)
            {
                if (item[i].Value == SystemFlag)
                {
                    // Debug.Log($"Removed {SystemFlag} from blockers on {entity}");
                    item.RemoveAt(i);
                    break;
                }
            }

            //Test if  the blocker is empty
            if (item.Length == 0)
            {
                EntityManager.RemoveComponent<ConsensusBlocker>(entity);
                Debug.Log($"Removed Blockers from {entity}");
                EntityManager.RemoveComponent<Disabled>(entity);
            }
        }

        ecb.Playback(EntityManager);

    }
}
