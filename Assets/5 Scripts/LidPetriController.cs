using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class LidPetriController : NetworkBehaviour
    {
        // References
        [SerializeField] private XRGrabInteractable grabInteractable;
        [SerializeField] private List<Collider> colliders;
        [SerializeField] private XRSocketInteractor socket;
        [SerializeField] private PetriController petriController;
        public PetriController PetriController => petriController;
        private PetriController originalPetri;

        // Network Variables
        private readonly NetworkVariable<bool> _isGrabbed =
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _detachable = 
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _petriIsAttached = 
            new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public bool isGrabbed => _isGrabbed.Value;
        
        // Initial state references
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Vector3 _initialScale;

        private bool _isOnline;
    
        
        // ------------------- NETWORK BEHAVIOUR METHODS -------------------
    
        private void Start()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _initialScale = transform.localScale;
            originalPetri = petriController;

            // Set interaction events
            grabInteractable.selectEntered.AddListener(Grab);
            grabInteractable.selectExited.AddListener(Drop);
            
            socket.selectEntered.AddListener(OnObjectPlaced);
            socket.selectExited.AddListener(OnObjectRemoved);
        }
        public override void OnDestroy()
        {
            // Remove interaction events
            grabInteractable.selectEntered.RemoveListener(Grab);
            grabInteractable.selectExited.RemoveListener(Drop);
            
            socket.selectEntered.RemoveListener(OnObjectPlaced);
            socket.selectExited.RemoveListener(OnObjectRemoved);
            
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
    
        
        // ------------------- INTERACTION METHODS -------------------

        
        private void Grab(SelectEnterEventArgs args)
        {
            // Ignore if not picked by hand
            if (!IsPickedByHand(args)) return;
            
            // Request Ownership to move the object
            RequestOwnershipServerRPC();
            
            // Set isGrabbed true
            SetIsGrabbedStateSynced(true);
            
            // Call grab in LineTask
            petriController.LineTask.Grab();

            if (_petriIsAttached.Value)
            {
                //Request ownership of petri
                petriController.RequestOwnershipServerRPC();
                
                // Change colliders
                if (_detachable.Value)
                {
                    SetCollidersStateSynced(false);
                    petriController.SetCollidersStateSynced(true);
                }
            }
        }
    
        private void Drop(SelectExitEventArgs args)
        {
            // Set isGrabbed false
            SetIsGrabbedStateSynced(false);
            
            // Call drop from the line task
            petriController.LineTask.Drop();
            
            // Change colliders
            // Skip if not detachable, colliders only change when detachable
            if (!_detachable.Value) return;
            SetCollidersStateSynced(true);
            if (_petriIsAttached.Value)
            {
                petriController.SetCollidersStateSynced(false);   
            }
        }
    
        
        // ------------------- SOCKET METHODS -------------------
    
        
        private void OnObjectPlaced(SelectEnterEventArgs args)
        {
            Debug.Log("[LidPetri]: Hello, something fell in my socket");
            if(_isOnline && !IsOwner) return;
            
            // Update references in LidPetri
            var petri = args.interactableObject.transform.GetComponentInChildren<PetriController>();
            if (petri is null) return;
            petriController = petri;
            
            // Update references in Petri
            SetPetriLidReferenceSynced();
            SetPetriParentSynced();
            SetPetriPositionTrackingSynced(false);
            SetPetriPhysicsStateSynced(false);
            SynchronizeTransformsPetriSocketSynced();
            
            // Set lpIsAttached true
            SetPetriIsAttachedStateSynced(true);
            
            // Avoid Colliders Overlapping
            Debug.Log($"[LidPetri]: I've attached a Petri, now I'm gonna avoid a colliders overlap. IsGrabbed state: {isGrabbed}");
            if(isGrabbed)
            {
                SetCollidersStateSynced(false);
            }
            else
            {
                SetPetriCollidersStateSynced(false);
            }
        }
        private void OnObjectRemoved(SelectExitEventArgs args)
        {
            Debug.Log("[LidPetri]: Petri was removed from socket");
            //if(_isOnline && !IsOwner) return;
            
            SetPetriIsAttachedStateSynced(false);
            DetachPetriSynced();
        }
        

        // ------------------- RPC METHODS -------------------

        
        [ServerRpc(RequireOwnership = false)]
        private void RequestOwnershipServerRPC(ServerRpcParams rpcParams = default)
        {
            // Change ownership
            ulong grabbingClientId = rpcParams.Receive.SenderClientId;
            GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);
        }
        
        // Set littlePetri parent as lid transform
        private void SetPetriParentSynced()
        {
            if (!_isOnline)
            {
                petriController.transform.SetParent(transform);
            }
            else
            {
                SetPetriParentRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetPetriParentRPC()
        {
            petriController.transform.SetParent(transform);
        }

        // Set littlePetri lid reference as this
        private void SetPetriLidReferenceSynced()
        {
            if (!_isOnline)
            {
                petriController.SetLidReference(this);
            }
            else
            {
                SetPetriLidReferenceRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetPetriLidReferenceRPC()
        {
            petriController.SetLidReference(this);
            petriController.transform.SetParent(transform);
        }
        
        // Set colliders' active state
        public void SetCollidersStateSynced(bool state)
        {
            if (!_isOnline)
            {
                foreach(var col in colliders)
                {
                    col.enabled = state;
                }   
            }
            else
            {
                SetCollidersStateRPC(state);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetCollidersStateRPC(bool state)
        {
            foreach(var col in colliders)
            {
                col.enabled = state;
            }
        }
        
        
        // Detach petri for everyone and set parent as null
        private void DetachPetriSynced()
        {
            if (!_isOnline)
            {
                // Set parent null
                petriController.transform.SetParent(null);
                // Activate position tracking
                petriController.SetNetworkComponentsStateSynced(true);
            }
            else
            {
                // Set parent null
                SetPetriParentNullRPC();
                // Activate position tracking
                SetPetriPositionTrackingRPC(true);
                
                if (!IsOwner) return;
                DetachInOthersRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetPetriParentNullRPC()
        {
            Debug.Log("[LidPetri]: I'm gonna set the petri's parent as null");
            petriController.transform.SetParent(null);
        }
        [Rpc(SendTo.NotMe)]
        private void DetachInOthersRPC()
        {
            Debug.Log("[LidPetri]: I'm not the owner, but I'm detaching anyways");
            var interactable = socket.GetOldestInteractableSelected();
            if (interactable != null)
            {
                socket.interactionManager.CancelInteractableSelection(interactable);
            }
        }

        private void SetPetriPositionTrackingSynced(bool state)
        {
            if (!_isOnline)
            {
                petriController.SetNetworkComponentsStateSynced(state);
            }
            else
            {
                SetPetriPositionTrackingRPC(state);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetPetriPositionTrackingRPC(bool state)
        {
            petriController.SetNetworkComponentsStateSynced(state);
        }
        
        private void SetPetriPhysicsStateSynced(bool state)
        {
            if (!_isOnline)
            {
                petriController.SetPhysicsStateSynced(state);
            }
            else
            {
                SetPetriPhysicsStateTrackingRPC(state);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetPetriPhysicsStateTrackingRPC(bool state)
        {
            petriController.SetPhysicsStateSynced(state);
        }
        
        private void SynchronizeTransformsPetriSocketSynced()
        {
            if (!_isOnline)
            {
                petriController.transform.position = socket.attachTransform.position;
                petriController.transform.rotation = socket.attachTransform.rotation;
            }
            else
            {
                SynchronizeTransformsPetriSocketRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SynchronizeTransformsPetriSocketRPC()
        {
            petriController.transform.position = socket.attachTransform.position;
            petriController.transform.rotation = socket.attachTransform.rotation;
        }
        
        private void SetPetriCollidersStateSynced(bool state)
        {
            if (!_isOnline)
            {
                petriController.SetCollidersStateSynced(state);
            }
            else
            {
                SetPetriCollidersStateRPC(state);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetPetriCollidersStateRPC(bool state)
        {
            petriController.SetCollidersStateSynced(state);
        }
        
        // ------------------- RPC ACCESS TO VARIABLES -------------------
        
        
        // _isGrabbed
        private void SetIsGrabbedStateSynced(bool state)
        {
            if (!_isOnline)
            {
                _isGrabbed.Value = state;   
            }
            else
            {
                SetIsGrabbedStateRPC(state);
            }
        }
        [Rpc(SendTo.Owner)]
        private void SetIsGrabbedStateRPC(bool state)
        {
            _isGrabbed.Value = state;
        }
        
        // _detachable
        public void SetDetachableStateSynced(bool state)
        {
            if (!_isOnline)
            {
                _detachable.Value = state;   
            }
            else
            {
                SetDetachableStateRPC(state);
            }
        }
        [Rpc(SendTo.Owner)]
        private void SetDetachableStateRPC(bool state)
        {
            _detachable.Value = state;
        }
        
        // _petriIsAttached
        private void SetPetriIsAttachedStateSynced(bool state)
        {
            if (_petriIsAttached.Value == state) return;
            
            if (!_isOnline)
            {
                _petriIsAttached.Value = state;   
            }
            else
            {
                SetPetriIsAttachedStateRPC(state);
            }
        }
        [Rpc(SendTo.Owner)]
        private void SetPetriIsAttachedStateRPC(bool state)
        {
            _petriIsAttached.Value = state;
        }
        
        
        // ------------------- OTHERS -------------------
    
        
        private bool IsPickedByHand(SelectEnterEventArgs args)
        {
            if (args.interactorObject.transform.CompareTag("LeftHand") ||
                args.interactorObject.transform.CompareTag("RightHand"))
            {
                return true;
            }
            return false;
        }

        
        
        // ------------------- RESTORATION -------------------


        public void RestoreToStartConditions()
        {
            // Physics
            SetCollidersStateSynced(true);
            StopRigidBodySynced();
            
            // Transform
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            transform.localScale = _initialScale;
            
            // Controller Variables
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
                // Assign reference to originalPetri
                petriController = originalPetri;
                
                // socket
                var gi = socket.GetOldestInteractableSelected();
                if (gi != null)
                {
                    socket.interactionManager.CancelInteractableSelection(gi);
                }

                ForceSocketAttach();
            }
            else
            {
                ResetComponentsRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void ResetComponentsRPC()
        {
            // Assign reference to originalPetri
            petriController = originalPetri;
            
            // socket
            var gi = socket.GetOldestInteractableSelected();
            if (gi != null)
            {
                socket.interactionManager.CancelInteractableSelection(gi);
            }
            ForceSocketAttach();
        }
        private void ForceSocketAttach()
        {
            var manager = socket.interactionManager;

            // If grabbed, release first
            if (petriController.GrabInteractable.isSelected && petriController.GrabInteractable.firstInteractorSelecting != null)
            {
                manager.SelectExit(petriController.GrabInteractable.firstInteractorSelecting, petriController.GrabInteractable);
            }

            // Attach to socket
            manager.SelectEnter((IXRSelectInteractor) socket, petriController.GrabInteractable);
        }

        // Network Variables
        private void ResetNetworkVariablesSynced()
        {
            if (!_isOnline)
            {
                _isGrabbed.Value = false;
                _detachable.Value = false;
                _petriIsAttached.Value = true;
            }
            else
            {
                ResetNetworkVariablesRPC();
            }
        }
        [Rpc(SendTo.Owner)]
        private void ResetNetworkVariablesRPC()
        {
            _isGrabbed.Value = false;
            _detachable.Value = false;
            _petriIsAttached.Value = true;
        }
    }
}
