using System.Collections.Generic;
using UnityEngine;

namespace UnityEngineExtensions
{
    public static class TransformExtensions
    {
        public static List<GameObject> FindChildrenWithTag(this Transform parent, string tag)
        {
            List<GameObject> taggedChildren = new List<GameObject>();
            foreach (Transform child in parent)
            {
                if (child.CompareTag(tag))
                {
                    taggedChildren.Add(child.gameObject);
                }
            }
            return taggedChildren;
        }

        public static GameObject FindChildWithTag(this Transform parent, string tag)
        {
            foreach (Transform child in parent)
            {
                if (child.CompareTag(tag))
                {
                    return child.gameObject;
                }
            }
            return null; // Return null if no child is found with the tag
        }
    }

    public static class GameObjectExtensions
    {
        public static Bounds GetBoundsWithChildren(this GameObject gameObj)
        {
            Bounds? bounds = null;
            Collider[] colliders = gameObj.GetComponentsInChildren<Collider>();
            foreach (Collider coll in colliders)
            {
                if (bounds.HasValue)
                {
                    bounds.Value.Encapsulate(coll.bounds);
                }
                else
                {
                    bounds = coll.bounds;
                }
            }

            return bounds.HasValue
                ? bounds.Value
                : new Bounds(gameObj.transform.position, Vector3.zero);
        }

        public static void SetLayer(this GameObject gameObj, int layer)
        {
            gameObj.layer = layer;
            foreach (Transform child in gameObj.transform)
            {
                child.gameObject.layer = layer;
            }
        }
    }
}
