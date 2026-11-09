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
        /// <summary>A student in VR: the headset rig and dissection tools, joining the professor's room.</summary>
        Student,
    }

    /// <summary>
    /// Lets the professor, student and classmates run the same scene. Netcode only keeps clients in step when they share a scene,
    /// and a body placed in three different scenes has three different ids. So there is one scene with all three setups in it, and
    /// this component, which runs before any other script, switches on the one that matches the device and switches off the
    /// other. The role comes from the app's package name (".classmate" or ".student", otherwise professor); in the editor it is a setting,
    /// which defaults to classmate because the Mac has no headset.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class ClassroomRole : MonoBehaviour
    {
        /// <summary>The package name suffix that marks the classmate phone build.</summary>
        public const string ClassmateIdSuffix = ".classmate";
        public const string StudentIdSuffix = ".student";

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

        [SerializeField, Tooltip("Objects used by both headset roles, including the XR origin.")]
        private GameObject[] _headsetObjects;
        [SerializeField, Tooltip("Behaviours used by both headset roles, including the XR UI module.")]
        private Behaviour[] _headsetBehaviours;
        [SerializeField, Tooltip("Join-only behaviours for the student and the phone classmate.")]
        private Behaviour[] _joiningBehaviours;
        [SerializeField] private Camera _headsetCamera;
        [SerializeField, Tooltip("The rig camera's AR background. Only the professor uses passthrough.")]
        private Behaviour _passthrough;

        /// <summary>Lets a test pick the role before a scene loads. Null means decide as usual. Never set outside tests.</summary>
        public static ClassroomRoleKind? OverrideForTests { get; set; }

        /// <summary>The role this device chose at start. Professor until a scene with this component has loaded.</summary>
        public static ClassroomRoleKind Current { get; private set; } = ClassroomRoleKind.Professor;

        /// <summary>The role for a package name. Pure, so it is tested without a device.</summary>
        public static ClassroomRoleKind Decide(string packageName, bool isEditor, ClassroomRoleKind editorRole)
        {
            if (isEditor)
                return editorRole;
            if (!string.IsNullOrEmpty(packageName))
            {
                if (packageName.EndsWith(ClassmateIdSuffix, System.StringComparison.Ordinal))
                    return ClassroomRoleKind.Classmate;
                if (packageName.EndsWith(StudentIdSuffix, System.StringComparison.Ordinal))
                    return ClassroomRoleKind.Student;
            }
            return ClassroomRoleKind.Professor;
        }

        private void Awake()
        {
            Current = OverrideForTests ?? Decide(Application.identifier, Application.isEditor, _editorRole);
            bool professor = Current == ClassroomRoleKind.Professor;
            SetActive(_professorObjects, professor);
            bool classmate = Current == ClassroomRoleKind.Classmate;
            SetActive(_classmateObjects, classmate);
            SetActive(_headsetObjects, !classmate);
            SetEnabled(_professorBehaviours, professor);
            SetEnabled(_classmateBehaviours, classmate);
            SetEnabled(_headsetBehaviours, !classmate);
            SetEnabled(_joiningBehaviours, !professor);
            if (_passthrough != null)
                _passthrough.enabled = professor;
            if (_headsetCamera != null)
            {
                if (Current == ClassroomRoleKind.Student)
                {
                    // Preserve the standalone student's fixed-height VR setup in the shared scene.
                    var origin = _headsetCamera.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
                    if (origin != null)
                    {
                        origin.RequestedTrackingOriginMode = Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Device;
                        origin.CameraYOffset = 1.6f;
                        Vector3 position = origin.transform.position;
                        origin.transform.position = new Vector3(position.x, 0f, position.z);
                    }
                }
                _headsetCamera.clearFlags = professor ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
                if (professor)
                    _headsetCamera.backgroundColor = Color.clear;
            }
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
