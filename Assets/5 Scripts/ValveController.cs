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
        [SerializeField] private AudioSource gasAudioSource;

        private bool _isOnline;
        
        // Initial state references
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Vector3 _initialScale;
        
    
        // NETWORK METHODS

        private void Start()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _initialScale = transform.localScale;
        }

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

        public void SetGasSoundStateSynced(bool state)
        {
            if (!_isOnline)
            {
                if (state)
                {
                    gasAudioSource.Play();
                }
                else
                {
                    gasAudioSource.Stop();
                }
            }
            else
            {
                
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetGasSoundStateRPC(bool state)
        {
            if (state)
            {
                gasAudioSource.Play();
            }
            else
            {
                gasAudioSource.Stop();
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
    
        private IEnumerator RotateCoroutine(bool newReleasingGas)
        {
            Quaternion startRot = transform.rotation;
            Quaternion endRot = newReleasingGas ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, -90, 0);

            if (newReleasingGas && (!_isOnline || (_isOnline && IsOwner)))
            {
                _releasingGas.Value = true;
                gasAudioSource.Play();
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
                _releasingGas.Value = newReleasingGas;
            }

            if (!newReleasingGas)
            {
                gasAudioSource.Stop();
            }

            // Snap to final rotation (avoids precision drift)
            transform.rotation = endRot;
        }
        
        // OTHERS
        
        public void RestoreToStartConditionsSynced()
        {
            // Controller Variables
            ResetVariablesAndCoroutinesSynced();
            // Transform
            SetInitialTransformSynced();
        }
        private void SetInitialTransformSynced()
        {
            if (!_isOnline)
            {
                // Transform
                transform.position = _initialPosition;
                transform.rotation = _initialRotation;
                transform.localScale = _initialScale;
            }
            else
            {
                SetInitialTransformRPC();
            }
        }
        [Rpc(SendTo.Owner)]
        private void SetInitialTransformRPC()
        {
            // Transform
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            transform.localScale = _initialScale;
        }
        
        // Network Variables
        private void ResetVariablesAndCoroutinesSynced()
        {
            if (!_isOnline)
            {
                _open.Value = false;
                _releasingGas.Value = false;
                gasAudioSource.Stop();
                StopAllCoroutines();
            }
            else
            {
                ResetNetworkVariablesRPC();
                StopAllCoroutinesRPC();
            }
        }
        [Rpc(SendTo.Owner)]
        private void ResetNetworkVariablesRPC()
        {
            _open.Value = false;
            _releasingGas.Value = false;
            gasAudioSource.Stop();
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void StopAllCoroutinesRPC()
        {
            StopAllCoroutines();
        }
    }
}