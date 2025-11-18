using Unity.Netcode;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class ModuleController : NetworkBehaviour
    {
        [SerializeField] private ClipboardController clipboard;

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
                clipboard.RestoreToStartConditions();
            }
            else
            {
                RestoreToolsRPC();
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void RestoreToolsRPC()
        {
            clipboard.RestoreToStartConditions();
        }
    }
}
