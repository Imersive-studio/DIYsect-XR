using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;

namespace DIYsect.Editor
{
    /// <summary>
    /// For the length of its lifetime the Android project settings describe a phone build: XR does not start,
    /// no XR loader is included, and the app has its own identifier and name so it installs next to the Quest app.
    /// Dispose puts every changed value back, so the Quest build is never affected.
    /// </summary>
    public sealed class PhoneBuildSettingsScope : IDisposable
    {
        public const string IdentifierSuffix = ".classmate";
        public const string PhoneProductName = "DIYsect Classmate";

        private readonly XRGeneralSettings _xrSettings;
        private readonly bool _initManagerOnStart;
        private readonly List<XRLoader> _loaders;
        private readonly string _identifier;
        private readonly string _productName;
        private readonly bool _overrideIdentifier;
        private bool _disposed;

        public PhoneBuildSettingsScope()
        {
            _identifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            _productName = PlayerSettings.productName;
            _overrideIdentifier = ReadOverrideIdentifier();
            _xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            try
            {
                if (_xrSettings != null)
                {
                    _initManagerOnStart = _xrSettings.InitManagerOnStart;
                    _loaders = new List<XRLoader>(_xrSettings.Manager.activeLoaders);
                    _xrSettings.InitManagerOnStart = false;
                    if (!_xrSettings.Manager.TrySetLoaders(new List<XRLoader>()))
                        throw new InvalidOperationException("Could not remove the Android XR loaders for the phone build.");
                }
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, _identifier + IdentifierSuffix);
                PlayerSettings.productName = PhoneProductName;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            if (_xrSettings != null && _loaders != null)
            {
                _xrSettings.InitManagerOnStart = _initManagerOnStart;
                _xrSettings.Manager.TrySetLoaders(_loaders);
            }
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, _identifier);
            PlayerSettings.productName = _productName;
            WriteOverrideIdentifier(_overrideIdentifier);
        }

        // SetApplicationIdentifier also switches on "override default identifier" and no PlayerSettings API
        // switches it back, so it is read and restored through the serialized settings.
        private const string OverrideIdentifierField = "overrideDefaultApplicationIdentifier";

        private static SerializedObject OpenPlayerSettings()
        {
            UnityEngine.Object[] settings = UnityEngine.Resources.FindObjectsOfTypeAll<PlayerSettings>();
            if (settings.Length == 0)
                throw new InvalidOperationException("PlayerSettings is not loaded.");
            return new SerializedObject(settings[0]);
        }

        private static bool ReadOverrideIdentifier()
        {
            using (SerializedObject owner = OpenPlayerSettings())
            {
                return FindOverrideIdentifier(owner).boolValue;
            }
        }

        private static void WriteOverrideIdentifier(bool value)
        {
            using (SerializedObject owner = OpenPlayerSettings())
            {
                FindOverrideIdentifier(owner).boolValue = value;
                owner.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static SerializedProperty FindOverrideIdentifier(SerializedObject owner) =>
            owner.FindProperty(OverrideIdentifierField)
            ?? throw new InvalidOperationException($"PlayerSettings has no {OverrideIdentifierField} field.");
    }
}
