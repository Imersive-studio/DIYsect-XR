// Authority: none. Gives this client's in-scene NetworkObjects the ids the server and the other clients use for them.
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace XRMultiplayer
{
    /// <summary>
    /// Lets the professor, student and classmate scenes share one body. Netcode finds an in-scene NetworkObject on another
    /// client by an id generated from the object's place in its own scene, so the same body placed in three different scenes
    /// has three different ids and never matches. With scene syncing off (<c>EnableSceneManagement</c>), each scene keeps its
    /// own content, and this component replaces every in-scene id with one made from the object's hierarchy path, which is
    /// the same in all three scenes. It runs before any other script, so the ids are set before a room is started or joined.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class StableNetworkObjectIds : MonoBehaviour
    {
        private static readonly FieldInfo s_IdField = typeof(NetworkObject).GetField(
            "GlobalObjectIdHash", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private void Awake()
        {
            Apply(gameObject.scene);
        }

        /// <summary>The id Netcode currently has for an in-scene object, or 0 when it cannot be read.</summary>
        public static uint GetId(NetworkObject networkObject)
        {
            return s_IdField != null ? (uint)s_IdField.GetValue(networkObject) : 0u;
        }

        /// <summary>Gives every NetworkObject in the scene an id made from its hierarchy path. Returns how many it set.</summary>
        public static int Apply(Scene scene)
        {
            if (s_IdField == null)
            {
                Debug.LogError("[StableNetworkObjectIds] NetworkObject has no GlobalObjectIdHash field in this Netcode version; scenes cannot share objects.");
                return 0;
            }

            int set = 0;
            var used = new Dictionary<uint, string>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (NetworkObject networkObject in root.GetComponentsInChildren<NetworkObject>(true))
                {
                    string path = PathOf(networkObject.transform);
                    uint id = HashOfPath(path);
                    if (used.TryGetValue(id, out string other))
                    {
                        Debug.LogError($"[StableNetworkObjectIds] {path} and {other} would share the id {id}.");
                        continue;
                    }
                    used[id] = path;
                    s_IdField.SetValue(networkObject, id);
                    set++;
                }
            }
            return set;
        }

        /// <summary>
        /// Hierarchy path from the scene root, such as "DIYsect/AnatomyBody". Siblings with the same name get their position
        /// added, so the path is unique and is the same wherever the same hierarchy is built.
        /// </summary>
        public static string PathOf(Transform transform)
        {
            var parts = new List<string>();
            for (Transform current = transform; current != null; current = current.parent)
                parts.Add(PartOf(current));
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string PartOf(Transform transform)
        {
            Transform parent = transform.parent;
            int sameName = 0;
            int position = 0;
            if (parent != null)
            {
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform sibling = parent.GetChild(i);
                    if (sibling.name != transform.name)
                        continue;
                    if (sibling == transform)
                        position = sameName;
                    sameName++;
                }
            }
            else
            {
                foreach (GameObject root in transform.gameObject.scene.GetRootGameObjects())
                {
                    if (root.name != transform.name)
                        continue;
                    if (root.transform == transform)
                        position = sameName;
                    sameName++;
                }
            }
            return sameName > 1 ? $"{transform.name}#{position}" : transform.name;
        }

        /// <summary>A 32-bit FNV-1a hash of the path. Never 0, because Netcode treats 0 as "no id".</summary>
        public static uint HashOfPath(string path)
        {
            uint hash = 2166136261u;
            foreach (byte b in Encoding.UTF8.GetBytes(path))
            {
                hash ^= b;
                hash *= 16777619u;
            }
            return hash == 0u ? 1u : hash;
        }
    }
}
