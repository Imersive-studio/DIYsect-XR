// Authority: none. Local to the professor's headset; it only changes how this headset draws the room.
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using XRMultiplayer;

namespace DIYsect.ClassRoster
{
    /// <summary>
    /// The professor's button for choosing how they see the room: the classroom the students are in, or mixed reality with the real
    /// room behind the body. It shows where the professor is now and what pressing it does, and flips between the two.
    /// </summary>
    public class ProfessorViewButton : MonoBehaviour
    {
        [SerializeField] private XRSimpleInteractable _button;
        [SerializeField] private TextMesh _label;

        private ClassroomRole _role;
        private UnityEngine.Events.UnityAction<SelectEnterEventArgs> _pressed;

        private void Awake()
        {
            if (_button == null || _label == null)
            {
                Debug.LogError($"{name}: ProfessorViewButton has no button or label. Run Tools > DIYsect > Build Shared Scene.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _role = FindFirstObjectByType<ClassroomRole>(FindObjectsInactive.Include);
            _pressed = _ => Press();
            _button.selectEntered.AddListener(_pressed);
            ClassroomRole.ProfessorViewChanged += OnChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (_button != null && _pressed != null)
                _button.selectEntered.RemoveListener(_pressed);
            ClassroomRole.ProfessorViewChanged -= OnChanged;
        }

        /// <summary>Flips to the other view.</summary>
        public void Press()
        {
            if (_role != null)
                _role.SetProfessorView(ProfessorViewRule.Other(_role.ProfessorView));
        }

        private void OnChanged(ProfessorViewMode mode) => Refresh();

        private void Refresh()
        {
            string text = ProfessorViewRule.ButtonText(_role != null ? _role.ProfessorView : ProfessorViewRule.Default);
            if (_label.text != text)
                _label.text = text;
        }
    }
}
