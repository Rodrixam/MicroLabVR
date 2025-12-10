using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class BeakGlassController : NetworkBehaviour
    {
        // References
        [SerializeField] private List<XRSocketInteractor> sockets;
        [SerializeField] private ParticleSystem bubbles;
        [SerializeField] private XRGrabInteractable interactable;
        [SerializeField] private AudioSource bubblesSound;
        [SerializeField] private AudioSource timerSound;
        private readonly List<TubeController> _attachedTubes =new();
        
        // Variables
        private readonly NetworkVariable<bool> _boiling = 
            new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        // Initial state references
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Vector3 _initialScale;
        
        private bool _isOnline;

        private void Start()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _initialScale = transform.localScale;
        }

        private void Awake()
        {
            foreach (var sock in sockets)
            {
                // Subscribe to socket events
                sock.selectEntered.AddListener(OnTubePlaced);
                sock.selectExited.AddListener(OnTubeRemoved);   
            }
            _boiling.OnValueChanged += BoilingStateChanged;
            
            interactable.selectEntered.AddListener(Grab);
        }

        public override void OnDestroy()
        {
            foreach (var sock in sockets)
            {
                // Always unsubscribe to avoid leaks
                sock.selectEntered.RemoveListener(OnTubePlaced);
                sock.selectExited.RemoveListener(OnTubeRemoved);
            }
            _boiling.OnValueChanged -= BoilingStateChanged;
            
            interactable.selectEntered.RemoveListener(Grab);
            base.OnDestroy();
        }

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
        
        // SOCKET EVENTS

        private void OnTubePlaced(SelectEnterEventArgs args)
        {
            if (_isOnline && !IsOwner) return;
            Debug.Log("[BeakGlass]: A tube fell into my socket. I'm Owner.");
            
            TubeController tube = args.interactableObject.transform.GetComponentInChildren<TubeController>();
            tube.SetColliderStateSynced(false, true);
            if (_boiling.Value)
            {
                tube.SetOnFireSynced(true);
            }

            StoreTubeReferenceSynced(tube);
        }

        private void OnTubeRemoved(SelectExitEventArgs args)
        {
            if (_isOnline && !IsOwner) return;
            Debug.Log("[BeakGlass]: A tube was removed from my socket. I'm Owner.");
            
            TubeController tube = args.interactableObject.transform.GetComponentInChildren<TubeController>();
            tube.SetColliderStateSynced(true, false);
            tube.SetOnFireSynced(false);

            RemoveTubeReferenceSynced(tube);
        }

        // ADD TUBES
        private void StoreTubeReferenceSynced(TubeController tube)
        {
            if (_attachedTubes.Contains(tube)) return;
            
            if (!_isOnline)
            {
                _attachedTubes.Add(tube);
            }
            else
            {
                StoreTubeReferenceRPC(tube.NetworkObject);
                //tube.RequestOwnershipServerRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void StoreTubeReferenceRPC(NetworkObjectReference tubeReference)
        {
            if (tubeReference.TryGet(out NetworkObject networkObject))
            {
                var tube = networkObject.GetComponent<TubeController>();
                _attachedTubes.Add(tube);
            }
        }
        // REMOVE TUBES
        private void RemoveTubeReferenceSynced(TubeController tube)
        {
            if (!_isOnline)
            {
                _attachedTubes.Remove(tube);
            }
            else
            {
                RemoveTubeReferenceRPC(tube.NetworkObject);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void RemoveTubeReferenceRPC(NetworkObjectReference tubeReference)
        {
            if (tubeReference.TryGet(out NetworkObject networkObject))
            {
                var tube = networkObject.GetComponent<TubeController>();
                _attachedTubes.Remove(tube);
            }
        }
        // CLEAR TUBES
        private void ClearTubeReferenceSynced()
        {
            if (!_isOnline)
            {
                _attachedTubes.Clear();
            }
            else
            {
                ClearTubeReferenceRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void ClearTubeReferenceRPC()
        {
            _attachedTubes.Clear();
        }

        // ON FIRE ACCESS METHODS
        
        public void SetOnFireStateSynced(bool value)
        {
            if (!_isOnline)
            {
                _boiling.Value = value;
                BoilingStateChanged(false, value);
                
                var tubes = GetTubes();
                foreach (var tube in tubes)
                {
                    tube.SetOnFireSynced(value);
                }
            }
            else
            {
                SetOnFireStateRPC(value);
            }
        }
        [Rpc(SendTo.Owner)]
        private void SetOnFireStateRPC(bool value)
        {
            _boiling.Value = value;
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        private List<TubeController> GetTubes()
        {
            List<TubeController> tubes = new ();
            foreach (var socket in sockets)
            {
                var tube = socket.firstInteractableSelected;
                
                GameObject obj = tube?.transform.gameObject;
                if (obj is not null)
                {
                    var tubeController = obj.GetComponent<TubeController>();
                    if(tubeController is not null)
                    {
                        tubes.Add(tubeController);
                    }
                }
            }

            return tubes;
        }

        // INTERACTION METHODS

        private void Grab(SelectEnterEventArgs args)
        {
            if (_isOnline && !IsOwner)
            {
                RequestOwnershipServerRPC();
            }
        }
        [ServerRpc(RequireOwnership = false)]
        private void RequestOwnershipServerRPC(ServerRpcParams rpcParams = default)
        {
            // Change ownership
            ulong grabbingClientId = rpcParams.Receive.SenderClientId;
            GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);

            foreach (var tube in _attachedTubes)
            {
                tube.RequestOwnershipServerRPC();
            }
        }

        private void BoilingStateChanged(bool oldValue, bool newValue)
        {
            // Particle System
            if (newValue)
            {
                bubbles.Play();
                bubblesSound.Play();
            }
            else
            {
                bubbles.Stop();
                bubblesSound.Stop();
                timerSound.Stop();
            }
            
            // Tubes attached
            foreach (var tube in _attachedTubes)
            {
                tube.SetOnFireSynced(newValue);
            }
        }

        public void AskTimerSoundSynced(bool state)
        {
            if (!_isOnline)
            {
                if(state)   TryTurnTimerOn();
                else        TryTurnTimerOff();
            }
            else
            {
                AskTimerSoundRPC(state);   
            }
        }
        [Rpc(SendTo.Owner)]
        private void AskTimerSoundRPC(bool state)
        {
            if(state)   TryTurnTimerOn();
            else        TryTurnTimerOff();
        }

        private void TryTurnTimerOn()
        {
            if (timerSound.isPlaying) return;
            timerSound.Play();
        }
        private void TryTurnTimerOff()
        {
            if (!timerSound.isPlaying) return;
            if (_attachedTubes.Any(tube => tube.OnHeat && tube.TubeTimer.interval < 3)) return;
            timerSound.Stop();
        }
        
        
        // ------------------- RESTORATION -------------------


        public void RestoreToStartConditions()
        {
            // Physics
            StopRigidBodySynced();
            
            // Transform
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            transform.localScale = _initialScale;
            
            // Controller Variables
            ClearTubeReferenceSynced();
            ResetNetworkVariablesSynced();
            ResetComponentsSynced();
        }
        
        // Physics
        private void StopRigidBodySynced()
        {
            if (!_isOnline)
            {
                var rBody = GetComponent<Rigidbody>();
                if (rBody is not null)
                {
                    rBody.linearVelocity = Vector3.zero;
                    rBody.angularVelocity = Vector3.zero;
                }
            }
            else
            {
                StopRigidBodyRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void StopRigidBodyRPC()
        {
            var rBody = GetComponent<Rigidbody>();
            if (rBody is not null)
            {
                rBody.linearVelocity = Vector3.zero;
                rBody.angularVelocity = Vector3.zero;
            }
        }
        
        // Components
        private void ResetComponentsSynced()
        {
            if (!_isOnline)
            {
                // sockets
                foreach (var socket in sockets)
                {
                    var gi = socket.GetOldestInteractableSelected();
                    if (gi != null)
                    {
                        socket.interactionManager.CancelInteractableSelection(gi);
                    }
                }
                // Bubbles
                bubbles.Stop();
            }
            else
            {
                ResetComponentsRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void ResetComponentsRPC()
        {
            // sockets
            foreach (var socket in sockets)
            {
                var gi = socket.GetOldestInteractableSelected();
                if (gi != null)
                {
                    socket.interactionManager.CancelInteractableSelection(gi);
                }
            }
            // Bubbles
            bubbles.Stop();
        }

        // Network Variables
        private void ResetNetworkVariablesSynced()
        {
            if (!_isOnline)
            {
                _boiling.Value = false;
            }
            else
            {
                ResetNetworkVariablesRPC();
            }
        }
        [Rpc(SendTo.Owner)]
        private void ResetNetworkVariablesRPC()
        {
            _boiling.Value = false;
        }
    }
}
