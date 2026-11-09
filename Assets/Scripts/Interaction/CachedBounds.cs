using UnityEngine;

namespace DIYsect.Interaction
{
    /// <summary>
    /// The world bounds of an organ's meshes, read every frame from a renderer list that is gathered once, so a label
    /// can follow a moving organ without searching its children again each frame.
    /// </summary>
    public sealed class CachedBounds
    {
        private readonly Transform _root;
        private readonly Renderer[] _renderers;

        public CachedBounds(Transform root)
        {
            _root = root;
            _renderers = root.GetComponentsInChildren<Renderer>(true);
        }

        /// <summary>The box around every enabled, active renderer now. A zero size box at the root when nothing renders.</summary>
        public Bounds Current()
        {
            bool found = false;
            var bounds = new Bounds(_root.position, Vector3.zero);
            foreach (Renderer renderer in _renderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return bounds;
        }
    }
}
