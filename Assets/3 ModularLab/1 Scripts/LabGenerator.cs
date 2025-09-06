using System;
using System.Net.Mime;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ModularLab.Scripts
{
    public class LabGenerator : NetworkBehaviour
    {
        [SerializeField] private string labSceneName;
        [SerializeField] private int moduleNum;
        [SerializeField] private Text uiText;

        private AsyncOperation _sceneLoadOp = null;
        private Scene? _labScene = null;

        
        private void OnNetworkSpawn()
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoaded;
        }

        private void OnSceneLoaded(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            _labScene = SceneManager.GetSceneByName(sceneName);

            Debug.Log($"Client {clientId} finished loading {sceneName}");
            Debug.Log(_labScene.Value.name);
        }

        public void LoadLabScene()
        {
            if (NetworkManager.Singleton.IsServer) // Only server/host does this
            {
                NetworkManager.Singleton.SceneManager.LoadScene(
                    labSceneName, 
                    LoadSceneMode.Additive
                );
            }
        }
        
        public void UnloadLabScene()
        {
            if (_labScene == null)
            {
                Debug.LogWarning("No LabScene Loaded");
                return;
            }
            
            if (NetworkManager.Singleton.IsServer) // Only server/host does this
            {
                NetworkManager.Singleton.SceneManager.UnloadScene(_labScene.Value);
            }
        }

        public void AddModule()
        {
            moduleNum++;
            UpdateText();
        }

        public void RemoveModule()
        {
            moduleNum--;
            UpdateText();
        }

        public void UpdateText()
        {
            uiText.text = moduleNum.ToString();
        }

        public int GetModuleNum()
        {
            return moduleNum;
        }
    }
}
