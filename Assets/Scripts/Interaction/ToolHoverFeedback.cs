// Authority: none. Local presentation only; it tints the tool and vibrates a controller on this device and sends nothing over the network.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Interaction
{
    /// <summary>The rule for the tool highlight: on while at least one hand hovers it and nobody holds it.</summary>
    internal static class ToolHoverRule
    {
        public static bool IsHighlighted(int hoverCount, bool selected)
        {
            return hoverCount > 0 && !selected;
        }
    }

    /// <summary>
    /// Makes a hand-held instrument easy to find and confirms the grab. While a hand points at the tool it is tinted
    /// and the controller gives a short tick; when the tool is grabbed the tint goes and the controller gives a
    /// firmer pulse. The tint is a MaterialPropertyBlock, so no material is copied and nothing is saved.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ToolHoverFeedback : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField, Tooltip("Tint shown while a hand points at the tool.")]
        private Color _hoverColor = new Color(1f, 0.92f, 0.45f, 1f);
        [SerializeField] private HapticPulse _hoverPulse = new HapticPulse(0.15f, 0.05f);
        [SerializeField] private HapticPulse _grabPulse = new HapticPulse(0.5f, 0.1f);

        private XRGrabInteractable _interactable;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private int _hoverCount;
        private bool _selected;
        private bool _highlighted;

        private void OnEnable()
        {
            _interactable = GetComponent<XRGrabInteractable>();
            _interactable.hoverEntered.AddListener(OnHoverEntered);
            _interactable.hoverExited.AddListener(OnHoverExited);
            _interactable.selectEntered.AddListener(OnSelectEntered);
            _interactable.selectExited.AddListener(OnSelectExited);
        }

        private void OnDisable()
        {
            if (_interactable != null)
            {
                _interactable.hoverEntered.RemoveListener(OnHoverEntered);
                _interactable.hoverExited.RemoveListener(OnHoverExited);
                _interactable.selectEntered.RemoveListener(OnSelectEntered);
                _interactable.selectExited.RemoveListener(OnSelectExited);
            }
            _hoverCount = 0;
            _selected = false;
            Restore();
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            _hoverCount++;
            if (!_selected)
                InteractorHaptics.Send(args.interactorObject, _hoverPulse);
            Refresh();
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {
            _hoverCount = Mathf.Max(0, _hoverCount - 1);
            Refresh();
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            _selected = true;
            InteractorHaptics.Send(args.interactorObject, _grabPulse);
            Refresh();
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            _selected = false;
            Refresh();
        }

        private void Refresh()
        {
            if (ToolHoverRule.IsHighlighted(_hoverCount, _selected))
                Highlight();
            else
                Restore();
        }

        internal void Highlight()
        {
            if (_renderers == null)
                _renderers = GetComponentsInChildren<Renderer>(true);
            if (_block == null)
                _block = new MaterialPropertyBlock();
            foreach (Renderer r in _renderers)
            {
                if (r == null)
                    continue;
                r.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, _hoverColor);
                _block.SetColor(ColorId, _hoverColor);
                r.SetPropertyBlock(_block);
            }
            _highlighted = true;
        }

        internal void Restore()
        {
            if (!_highlighted)
                return;
            _highlighted = false;
            if (_renderers == null)
                return;
            foreach (Renderer r in _renderers)
            {
                if (r != null)
                    r.SetPropertyBlock(null);
            }
        }
    }
}
