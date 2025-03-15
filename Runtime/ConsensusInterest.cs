using Unity.Collections;
using Unity.Entities;

//A system may register itself as an interested party
//If an entity meets the Query, add a blocker with a value of SystemFlag
//Also add the actual type indicated by SystemFlag
//The system will eventually get to analyzing the entity
//Once the system is satisfied, it will remove its blocker from the DynamicBuffer
//WHen the buffer is empty, it is removed
//When no buffer remains, the entity is no longer suppressed/disabled
public partial class ConsensusInterest : IComponentData
{
    public EntityQuery Query;
    public ComponentType SystemFlag;
}

