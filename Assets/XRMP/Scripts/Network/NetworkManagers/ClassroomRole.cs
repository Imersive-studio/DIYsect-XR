// Authority: none. Local setup of this device's scene; nothing is sent over the network.
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>What this device is in the class.</summary>
    public enum ClassroomRoleKind
    {
        /// <summary>The professor, in a headset: the mixed reality rig, the roster, the student window and the table alignment.</summary>
        Professor,
        /// <summary>A classmate on a phone or laptop: a plain camera that orbits the body and taps to pin questions.</summary>
        Classmate,
    }

    /// <summary>
    /// Lets the professor and the classmates run the same scene. Netcode only keeps clients in step when they share a scene,
    /// and a body placed in three different scenes has three different ids. So there is one scene with both setups in it, and
    /// this component, which runs before any other script, switches on the one that matches the device and switches off the
    /// other. The role comes from the app's package name (the phone build ends in ".classmate"); in the editor it is a setting,
    /// which defaults to classmate because the Mac has no headset.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class ClassroomRole : MonoBehaviour
    {
        /// <summary>The package name suffix that marks the classmate phone build.</summary>
        public const string ClassmateIdSuffix = ".classmate";

        [SerializeField, Tooltip("The role in the Unity editor, where there is no package name to go by.")]
        private ClassroomRoleKind _editorRole = ClassroomRoleKind.Classmate;
        [SerializeField, Tooltip("Objects only the professor's headset uses.")]
        private GameObject[] _professorObjects;
        [SerializeField, Tooltip("Objects only a classmate uses.")]
        private GameObject[] _classmateObjects;
        [SerializeField, Tooltip("Scripts only the professor's headset runs.")]
        private Behaviour[] _professorBehaviours;
        [SerializeField, Tooltip("Scripts only a classmate runs.")]
        private Behaviour[] _classmateBehaviours;

        /// <summary>Lets a test pick the role before a scene loads. Null means decide as usual. Never set outside tests.</summary>
        public static ClassroomRoleKind? OverrideForTests { get; set; }

        /// <summary>The role this device chose at start. Professor until a scene with this component has loaded.</summary>
        public static ClassroomRoleKind Current { get; private set; } = ClassroomRoleKind.Professor;

        /// <summary>The role for a package name. Pure, so it is tested without a device.</summary>
        public static ClassroomRoleKind Decide(string packageName, bool isEditor, ClassroomRoleKind editorRole)
        {
            if (isEditor)
                return editorRole;
            return !string.IsNullOrEmpty(packageName) && packageName.EndsWith(ClassmateIdSuffix, System.StringComparison.Ordinal)
                ? ClassroomRoleKind.Classmate
                : ClassroomRoleKind.Professor;
        }

        private void Awake()
        {
            Current = OverrideForTests ?? Decide(Application.identifier, Application.isEditor, _editorRole);
            bool professor = Current == ClassroomRoleKind.Professor;
            SetActive(_professorObjects, professor);
            SetActive(_classmateObjects, !professor);
            SetEnabled(_professorBehaviours, professor);
            SetEnabled(_classmateBehaviours, !professor);
            Debug.Log($"[ClassroomRole] This device is the {Current}.");
        }

        private static void SetActive(GameObject[] objects, bool active)
        {
            if (objects == null)
                return;
            foreach (GameObject target in objects)
                if (target != null)
                    target.SetActive(active);
        }

        private static void SetEnabled(Behaviour[] behaviours, bool enabled)
        {
            if (behaviours == null)
                return;
            foreach (Behaviour target in behaviours)
                if (target != null)
                    target.enabled = enabled;
        }
    }
}
