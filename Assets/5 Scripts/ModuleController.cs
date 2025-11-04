using Unity.Netcode;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class ModuleController : NetworkBehaviour
    {
        [SerializeField] private GameObject toolsPrefab;
        [SerializeField] private Transform toolsInstantiated;

        private bool _isOnline;
        
        // NETWORK BEHAVIOUR METHODS

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _isOnline = true;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _isOnline = false;
        }
        
        // OTHERS

        public void RestoreTools()
        {
            if (!_isOnline)
            {
                Destroy(toolsInstantiated.gameObject);
                toolsInstantiated = Instantiate(toolsPrefab, transform).transform;   
            }
            else
            {
                RestoreToolsRPC();
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void RestoreToolsRPC()
        {
            Destroy(toolsInstantiated.gameObject);
            toolsInstantiated = Instantiate(toolsPrefab, transform).transform;
        }
    }
}
