using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModularLab.Scripts
{
    public class LabGenerator : MonoBehaviour
    {
        [SerializeField] private string labSceneName;
        [SerializeField] private int moduleNum;

        private AsyncOperation _sceneLoadOp = null;
        private Scene? _lobbyScene = null;

        private void Update()
        {
            // The lab scene must be loaded
            if (_sceneLoadOp is not { isDone: true }) return;
            
            // Exception case
            if (_lobbyScene == null)
            {
                Debug.LogError("No lobby scene.");
                return;
            }
            
            // Unload current scene
            SceneManager.UnloadSceneAsync((Scene)_lobbyScene);
        }

        public void LoadLabScene()
        {
            _lobbyScene = SceneManager.GetActiveScene();
            _sceneLoadOp = SceneManager.LoadSceneAsync(labSceneName, LoadSceneMode.Additive);
        }

        public void AddModule()
        {
            moduleNum++;
        }

        public void RemoveModule()
        {
            moduleNum--;
        }

        public int GetModuleNum()
        {
            return moduleNum;
        }
    }
}
