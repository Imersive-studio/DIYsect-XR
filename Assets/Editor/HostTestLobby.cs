// Run Tools > DIYsect > Host Test Lobby while a scene is playing in the editor.
using UnityEditor;
using UnityEngine;
using XRMultiplayer;

namespace DIYsect.Editor
{
    /// <summary>
    /// Creates a lobby from the Unity editor, so a classmate phone has something to join when no headset is hosting.
    /// The headset scenes only offer a VR menu for this, which the editor cannot click. Editor only: it is not part of
    /// any build. The menu entry is enabled once the playing scene has signed in to Unity Gaming Services.
    /// </summary>
    public static class HostTestLobby
    {
        private const string MenuPath = "Tools/DIYsect/Host Test Lobby (Play Mode)";
        private const string RoomName = "DIYsect test room";

        [MenuItem(MenuPath)]
        public static void Host()
        {
            XRINetworkGameManager manager = XRINetworkGameManager.Instance;
            if (manager == null)
            {
                Debug.LogError("[HostTestLobby] No XRINetworkGameManager. Press Play in a DIYsect scene first.");
                return;
            }
            Debug.Log($"[HostTestLobby] Creating lobby \"{RoomName}\".");
            manager.CreateNewLobby(RoomName);
        }

        [MenuItem(MenuPath, true)]
        private static bool CanHost()
        {
            return Application.isPlaying
                && XRINetworkGameManager.Instance != null
                && XRINetworkGameManager.CurrentConnectionState.Value == XRINetworkGameManager.ConnectionState.Authenticated;
        }
    }
}
