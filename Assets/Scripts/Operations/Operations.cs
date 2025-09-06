using UnityEngine;

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
        public string ID;
        public Vector3 location;

        public AttachedObjectDetails(string id, Vector3 loc)
        {
            ID = id;
            location = loc;
        }
    }

    /// <summary>
    /// Parent class for all operations
    /// </summary>
    public abstract class Operation : MonoBehaviour
    {
        public virtual AttachedObjectDetails attachedObjectDetails { get; set; }
        public abstract void execute();
    }
}
