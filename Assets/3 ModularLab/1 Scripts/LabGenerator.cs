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

        private bool _isOnline;


        private void Start()
        {
            UpdateText();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoaded;

            _isOnline = true;
        }

        public override void OnNetworkDespawn()
        {
            _isOnline = false;
        }

        private void OnSceneLoaded(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            _labScene = SceneManager.GetSceneByName(sceneName);

            Debug.Log($"Client {clientId} finished loading {sceneName}");
            Debug.Log(_labScene.Value.name);
        }

        public void ToggleLabLoadUnload()
        {
            if (_labScene is null)
            {
                LoadLabScene();
            }
            else
            {
                UnloadLabScene();
            }
        }

        public void LoadLabScene()
        {
            if (_isOnline)
            {
                if (NetworkManager.Singleton.IsServer) // Only server/host does this
                {
                    NetworkManager.Singleton.SceneManager.LoadScene(
                        labSceneName, 
                        LoadSceneMode.Additive
                    );
                }   
            }
            else if (_sceneLoadOp is null)
            {
                _sceneLoadOp = SceneManager.LoadSceneAsync(labSceneName, LoadSceneMode.Additive);
                
                if (_sceneLoadOp != null)
                {
                    _sceneLoadOp.completed += (op) =>
                    {
                        _labScene = SceneManager.GetSceneByName(labSceneName);
                        _sceneLoadOp = null;
                    };   
                }
            }
        }
        
        private void UnloadLabScene()
        {
            if (_labScene == null)
            {
                Debug.LogWarning("No LabScene Loaded");
                return;
            }

            if (_isOnline)
            {
                // Only server/host does this
                if (!NetworkManager.Singleton.IsServer) return;
                
                NetworkManager.Singleton.SceneManager.UnloadScene(_labScene.Value);
                _labScene = null;
            }
            else
            {
                SceneManager.UnloadSceneAsync(_labScene.Value);
                _labScene = null;
            }
        }

        public void AddModule()
        {
            if (moduleNum >= 9) return;
            
            moduleNum++;
            UpdateText();
        }

        public void RemoveModule()
        {
            if (moduleNum <= 0) return;
            
            moduleNum--;
            UpdateText();
        }

        private void UpdateText()
        {
            uiText.text = moduleNum.ToString();
        }

        public int GetModuleNum()
        {
            return moduleNum;
        }
    }
}
