using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Operations are environment changes (such as spawning a goal) that can be attached to interactive items
/// </summary>
namespace Operations
{
    /// <summary>
    /// Structure containing details about the object this operation is attached to
    /// </summary>
    public struct AttachedObjectDetails
    {
        public MonoBehaviour obj;
        public string ID;
        public Vector3 location;
    }

    /// <summary>
    /// Parent class for all operations
    /// </summary>
    public abstract class Operation : MonoBehaviour
    {
        private AttachedObjectDetails _attachedObjectDetails;
        public virtual AttachedObjectDetails attachedObjectDetails
        {
            get { return _attachedObjectDetails; }
        }

        public virtual void initialise(AttachedObjectDetails attachedObjectDetails)
        {
            _attachedObjectDetails = attachedObjectDetails;
        }

        /// <summary>
        /// Perform the operation. <paramref name="triggeringAgent"/> is the agent that pressed the button or
        /// entered the zone.
        /// </summary>
        public abstract void execute(TrainingAgent triggeringAgent);
        public virtual void completeExecution(TrainingAgent triggeringAgent) {}
    }

    /// <summary>
    /// Registry for operation mappings
    /// </summary>
    public static class OperationRegistry
    {
        /// <summary>
        /// Tag mappings used for YAML Deserialization
        /// </summary>
        public static readonly Dictionary<string, Type> OperationTagMappings = new Dictionary<string, Type>
        {
            {"!endEpisode", typeof(EndEpisode)},
            {"!toggleObject", typeof(ToggleObject)},
            {"!operationFromList", typeof(OperationFromList)},
            {"!limitedInvocationsOperation", typeof(LimitedInvocationsOperation)},
            {"!noneOperation", typeof(NoneOperation)},
            {"!grantReward", typeof(GrantReward)},
            {"!grantContinuousReward", typeof(GrantContinuousReward)},
            {"!freezeAgent", typeof(FreezeAgent)},
        };
    }
}
