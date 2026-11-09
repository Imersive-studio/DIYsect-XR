using System;
using UnityEditor;
using UnityEditor.Build;

namespace DIYsect.Editor
{
    /// <summary>
    /// For the length of its lifetime the Android project settings carry a role's own application id and name, so the
    /// professor and student apps install next to each other and next to the classmate app on one headset. XR stays on.
    /// Dispose puts the id, the name and the "override default identifier" switch back, so the all-scenes Quest build is
    /// never affected.
    /// </summary>
    public sealed class RoleBuildSettingsScope : IDisposable
    {
        private const string OverrideIdentifierField = "overrideDefaultApplicationIdentifier";

        private readonly string _identifier;
        private readonly string _productName;
        private readonly bool _overrideIdentifier;
        private bool _disposed;

        public RoleBuildSettingsScope(string identifierSuffix, string productName)
        {
            if (string.IsNullOrEmpty(identifierSuffix) || identifierSuffix[0] != '.')
                throw new ArgumentException("The identifier suffix must start with a dot, such as \".professor\".", nameof(identifierSuffix));
            _identifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            _productName = PlayerSettings.productName;
            _overrideIdentifier = ReadOverrideIdentifier();
            try
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, _identifier + identifierSuffix);
                PlayerSettings.productName = productName;
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
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, _identifier);
            PlayerSettings.productName = _productName;
            WriteOverrideIdentifier(_overrideIdentifier);
        }

        // SetApplicationIdentifier also switches on "override default identifier" and no PlayerSettings API switches it
        // back, so it is read and restored through the serialized settings.
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
                return Find(owner).boolValue;
        }

        private static void WriteOverrideIdentifier(bool value)
        {
            using (SerializedObject owner = OpenPlayerSettings())
            {
                Find(owner).boolValue = value;
                owner.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static SerializedProperty Find(SerializedObject owner) =>
            owner.FindProperty(OverrideIdentifierField)
            ?? throw new InvalidOperationException($"PlayerSettings has no {OverrideIdentifierField} field.");
    }
}
