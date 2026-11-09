using UnityEngine;

namespace DIYsect.Learning
{
    [ExecuteAlways]
    public class ChecklistBacking : MonoBehaviour
    {
        [SerializeField] private Renderer _renderer;

        private void OnEnable()
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            if (_renderer == null)
            {
                Debug.LogError($"{name}: checklist backing needs a renderer.", this);
                enabled = false;
                return;
            }
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", new Color(0.035f, 0.05f, 0.065f, 1f));
            _renderer.SetPropertyBlock(block);
        }
    }
}
