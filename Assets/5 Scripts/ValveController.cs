using System.Collections;
using Unity.Netcode;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class ValveController : NetworkBehaviour
    {
        private readonly NetworkVariable<bool> _open = 
            new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
        private readonly NetworkVariable<bool> _releasingGas = 
            new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public bool releasingGas => _releasingGas.Value;

        [SerializeField] private float duration;

        private bool _isOnline;

    
        // NETWORK METHODS
    
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _isOnline = true;
            _open.OnValueChanged += CallRotateCoroutine;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _isOnline = false;
            _open.OnValueChanged -= CallRotateCoroutine;
        }

        // INTERACTION METHODS
    
        public void Grab()
        {   
            // ------------------------- LOCAL -------------------------
            if (!_isOnline)
            {
                _open.Value = !_open.Value;
                CallRotateCoroutine(false,_open.Value);
            }
            // ------------------------- ONLINE -------------------------
            else
            {
                RequestGrabServerRPC(!_open.Value);
            }
        }
    
        // RPC METHODS
    
        [ServerRpc(RequireOwnership = false)]
        private void RequestGrabServerRPC(bool value, ServerRpcParams rpcParams = default)
        {   
            // Change ownership
            ulong grabbingClientId = rpcParams.Receive.SenderClientId;
            GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);
            
            // Change variables
            RotateValveRPC(value);
        }
    
        [Rpc(SendTo.Owner)]
        private void RotateValveRPC(bool newValue)
        {            
            _open.Value = newValue;
        }
        
        // COROUTINES

        // Synced through _open.OnValueChanged
        private void CallRotateCoroutine(bool previous, bool current)
        {
            StopAllCoroutines();
            StartCoroutine(RotateCoroutine(current));
        }
    
        private IEnumerator RotateCoroutine(bool newValue)
        {
            Quaternion startRot = transform.rotation;
            Quaternion endRot = newValue ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, -90, 0);

            if (!_isOnline || (_isOnline && IsOwner && newValue))
            {
                _releasingGas.Value = newValue;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                transform.rotation = Quaternion.Slerp(startRot, endRot, t);

                yield return null;
            }
        
            if (!_isOnline || (_isOnline && IsOwner))
            {
                _releasingGas.Value = newValue;
            }

            // Snap to final rotation (avoids precision drift)
            transform.rotation = endRot;
        }
    }
}