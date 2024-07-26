using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Facilitates the management and retrieval of Fade components associated with a collection of GameObjects, black screens.
/// </summary>
namespace Holders
{
    class PositionRotation
    {
        public Vector3 Position { get; }
        public Vector3 Rotation { get; }

        public PositionRotation(Vector3 position, Vector3 rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }

    [System.Serializable]
    public class ListOfBlackScreens
    {
        public List<GameObject> allBlackScreens;

        public List<Fade> GetFades()
        {
            if (allBlackScreens == null)
            {
                Debug.LogWarning("List of black screens is null.");
                return new List<Fade>();
            }

            /* Use LINQ to get Fade components from allBlackScreens: */
            return (
                from blackScreen in allBlackScreens
                let fadeComponent = blackScreen.GetComponentInChildren<Fade>()
                where fadeComponent != null
                select fadeComponent
            ).ToList();
        }
    }
}
