using System.Collections.Generic;
using UnityEngine;

namespace XRMultiplayer
{
    public class WorldCanvas : MonoBehaviour
    {
        [SerializeField] Transform m_PlayerNameTagsParent;
        [SerializeField] float m_nameTagOffsetHeight = 0.3f;
        readonly Dictionary<PlayerNameTag, XRINetworkPlayer> playerDictionary = new Dictionary<PlayerNameTag, XRINetworkPlayer>();

        private void Start()
        {
            XRINetworkGameManager.Connected.Subscribe(OnConnectedUpdate);
            XRINetworkGameManager.Instance.playerStateChanged += ConnectedPlayerStateChange;
        }

        private void OnDestroy()
        {
            XRINetworkGameManager.Connected.Unsubscribe(OnConnectedUpdate);
            XRINetworkGameManager.Instance.playerStateChanged -= ConnectedPlayerStateChange;
        }

        void OnConnectedUpdate(bool connected)
        {
            if (!connected)
            {
                foreach (var kvp in playerDictionary)
                {
                    // A name tag whose player already left is destroyed already.
                    if (kvp.Key != null)
                        Destroy(kvp.Key.gameObject);
                }
                playerDictionary.Clear();
            }
        }

        void ConnectedPlayerStateChange(ulong playerId, bool connected)
        {
            if (!connected)
            {
                if (!RemovePlayerNameTag(playerId))
                {
                    Utils.Log($"Failed to Remove Player with id {playerId}.", 1);
                }
            }
        }

        bool RemovePlayerNameTag(ulong playerId)
        {
            foreach (var key in playerDictionary.Keys)
            {
                if (key.playerId == playerId)
                {
                    playerDictionary.Remove(key);
                    Destroy(key.gameObject);

                    return true;
                }
            }
            return false;
        }

        public void SetupPlayerNameTag(XRINetworkPlayer player, PlayerNameTag nameTag)
        {
            nameTag.SetupNameTag(player);
            nameTag.transform.SetParent(m_PlayerNameTagsParent);

            if (!playerDictionary.ContainsKey(nameTag))
            {
                playerDictionary.Add(nameTag, player);
            }

            if (player.IsLocalPlayer)
            {
                nameTag.gameObject.SetActive(false);
            }
        }

        private readonly List<PlayerNameTag> m_StaleNameTags = new List<PlayerNameTag>();

        private void Update()
        {
            foreach (var kvp in playerDictionary)
            {
                // A player that left, or one with no head (a phone or laptop), is destroyed or has nothing to follow. Without
                // this check the loop threw on every frame for as long as the entry stayed in the dictionary.
                if (kvp.Key == null || kvp.Value == null || kvp.Value.head == null)
                {
                    m_StaleNameTags.Add(kvp.Key);
                    continue;
                }
                kvp.Key.transform.position = kvp.Value.head.position + Vector3.up * m_nameTagOffsetHeight;
                kvp.Key.UpdateVoice(kvp.Value.playerVoiceAmp);
            }

            if (m_StaleNameTags.Count == 0)
                return;
            foreach (PlayerNameTag stale in m_StaleNameTags)
                playerDictionary.Remove(stale);
            m_StaleNameTags.Clear();
        }
    }
}
