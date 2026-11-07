// Authority: none. Presentation only.
using System.Collections;
using DIYsect.Networking;
using UnityEngine;

namespace DIYsect.Interaction
{
    /// <summary>
    /// A see-through copy of one organ's meshes at its socket. It shares the organ's meshes and one material,
    /// so it costs no memory beyond a few objects. It is shown while the organ is held and hidden afterwards.
    /// After a release it flashes a colour (green accepted, red rejected) for a moment, then hides.
    /// The root stays active so the flash can run as a coroutine; only the mesh container toggles.
    /// </summary>
    public class OrganGhost : MonoBehaviour
    {
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _colorId = Shader.PropertyToID("_Color");

        private GameObject _meshes;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private Coroutine _flash;
        private bool _held;

        public bool IsShown => _meshes != null && _meshes.activeSelf;

        /// <summary>The colour of the flash that is playing, or null when the ghost has its normal colour.</summary>
        public Color? FlashColor { get; private set; }

        /// <summary>Builds a hidden ghost beside the organ, at the organ's home pose. Returns null when the organ has no meshes.</summary>
        public static OrganGhost Create(Transform organ, ISnappable snappable, Material material)
        {
            MeshFilter[] filters = organ.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0)
                return null;

            var root = new GameObject($"Ghost {organ.name}");
            root.transform.SetParent(organ.parent, false);
            root.transform.SetLocalPositionAndRotation(snappable.HomeRootPose.position, snappable.HomeRootPose.rotation);
            root.transform.localScale = organ.localScale;

            var meshes = new GameObject("Meshes");
            meshes.transform.SetParent(root.transform, false);
            var renderers = new Renderer[filters.Length];
            for (int i = 0; i < filters.Length; i++)
            {
                // The mesh's pose relative to the organ, so the copy keeps the same shape at the socket.
                Matrix4x4 relative = organ.worldToLocalMatrix * filters[i].transform.localToWorldMatrix;
                var part = new GameObject(filters[i].name);
                part.transform.SetParent(meshes.transform, false);
                part.transform.SetLocalPositionAndRotation(relative.GetPosition(), relative.rotation);
                part.transform.localScale = relative.lossyScale;
                part.AddComponent<MeshFilter>().sharedMesh = filters[i].sharedMesh;
                var meshRenderer = part.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = material;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                renderers[i] = meshRenderer;
            }

            var ghost = root.AddComponent<OrganGhost>();
            ghost._meshes = meshes;
            ghost._renderers = renderers;
            ghost._block = new MaterialPropertyBlock();
            meshes.SetActive(false);
            return ghost;
        }

        /// <summary>Shows the ghost while the organ is held, or hides it unless a flash is still playing.</summary>
        public void SetHeld(bool held)
        {
            _held = held;
            if (held)
                StopFlash();
            _meshes.SetActive(held || _flash != null);
        }

        /// <summary>Shows the ghost in green or red for <paramref name="seconds"/>, then hides it unless the organ is held again.</summary>
        public void Flash(Color color, float seconds)
        {
            StopFlash();
            FlashColor = color;
            Tint(color);
            _meshes.SetActive(true);
            _flash = StartCoroutine(EndFlash(seconds));
        }

        private IEnumerator EndFlash(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _flash = null;
            FlashColor = null;
            ClearTint();
            _meshes.SetActive(_held);
        }

        private void StopFlash()
        {
            if (_flash != null)
                StopCoroutine(_flash);
            _flash = null;
            FlashColor = null;
            ClearTint();
        }

        private void Tint(Color color)
        {
            foreach (Renderer part in _renderers)
            {
                part.GetPropertyBlock(_block);
                _block.SetColor(_baseColorId, color);
                _block.SetColor(_colorId, color);
                part.SetPropertyBlock(_block);
            }
        }

        private void ClearTint()
        {
            foreach (Renderer part in _renderers)
                part.SetPropertyBlock(null);
        }
    }
}
