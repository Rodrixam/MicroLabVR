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
        private Rigidbody _rBody;

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

            _rBody = GetComponent<Rigidbody>();
            _rBody.isKinematic = false;
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

        // GRAB
        private void Grab(SelectEnterEventArgs args)
        {
            // Ignore if not picked by hand
            if (!IsPickedByHand(args)) return;
            
            // Request Ownership to move the object
            if (_isOnline)
            {
                RequestOwnershipServerRPC();   
            }
            
            // Set isGrabbed true
            SetIsGrabbedStateSynced(true);
            
            // Call grab in LineTask
            petriController.LineTask.GrabSynced();

            if (_petriIsAttached.Value)
            {
                //Request ownership of petri
                if (_isOnline)
                {
                    petriController.RequestOwnershipServerRPC();
                }
                
                // Change colliders
                if (_detachable.Value)
                {
                    SetCollidersStateSynced(false);
                    petriController.SetCollidersStateSynced(true);
                }
            }
        }
        [ServerRpc(RequireOwnership = false)]
        private void RequestOwnershipServerRPC(ServerRpcParams rpcParams = default)
        {
            // Change ownership
            ulong grabbingClientId = rpcParams.Receive.SenderClientId;
            GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);

            if (_petriIsAttached.Value)
            {
                petriController.RequestOwnershipServerRPC();
            }
        }
        [Rpc(SendTo.Owner)]
        private void MakeOwnerRequestPetriOwnershipRPC()
        {
            petriController.RequestOwnershipServerRPC();
        }
        
        // DROP
        private void Drop(SelectExitEventArgs args)
        {
            // Set isGrabbed false
            SetIsGrabbedStateSynced(false);
            
            // Call drop from the line task
            petriController.LineTask.DropSynced();
            
            // Change colliders
            if (!_detachable.Value) return;
            SetCollidersStateSynced(true);
            if (_petriIsAttached.Value)
            {
                petriController.SetCollidersStateSynced(false);   
            }
        }
    
        
        // ------------------- SOCKET METHODS -------------------

        // ON OBJECT PLACED
        private void OnObjectPlaced(SelectEnterEventArgs args)
        {
            Debug.Log("[LidPetri]: I'm gonna attach something");
            if (_petriIsAttached.Value)
            {
                Debug.Log("[LidPetri]: Something fell in my socket, but I already had something attached");
                return;
            }

            // Update references in LidPetri
            var petri = args.interactableObject.transform.GetComponentInChildren<PetriController>();
            if (petri is null) return;
            UpdatePetriReferenceSynced(petri);

            // Update references in Petri
            SetPetriLidReferenceSynced();
            SetPetriParentSynced();
            
            //petriController.SetNetworkComponentsStateSynced(false);
            petriController.SetPhysicsStateSynced(false);
            SynchronizeTransformsPetriSocketSynced();// -   transform might not be necessary to sync

            // Set lpIsAttached true
            SetPetriIsAttachedStateSynced(true);

        
            // Avoid Colliders Overlapping
            if (isGrabbed)
            {
                SetCollidersStateSynced(false);
            }
            else
            {
                petriController.SetCollidersStateSynced(false);
            }
            
            // Online syncing
            if (_isOnline)
            {
                // Update ownership of little petri
                MakeOwnerRequestPetriOwnershipRPC();   
                
                // Sync socket holding
                AttachInOthersRPC(petri.NetworkObject);
            }
        }
        
        // ON OBJECT REMOVED
        private void OnObjectRemoved(SelectExitEventArgs args)
        {
            Debug.Log($"[LidPetri]: An object was removed. petriIsAttached? {_petriIsAttached.Value}");
            // Change petriIsAttached value
            if (!_petriIsAttached.Value) return;
            SetPetriIsAttachedStateSynced(false);
            
            // Activate position tracking
            // petriController.SetNetworkComponentsStateSynced(true);
            // petriController.SetPhysicsStateSynced(false);
            
            if (!_isOnline)
            {
                // Set parent null
                petriController.transform.SetParent(null);
            }
            else
            {
                // Set parent null
                SetPetriParentNullRPC();
                // Sync detach
                DetachInOthersRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetPetriParentNullRPC()
        {
            petriController.transform.SetParent(null);
        }

        // ------------------- RPC METHODS -------------------
        
        // Set petriController reference
        private void UpdatePetriReferenceSynced(PetriController petri)
        {
            if (!_isOnline)
            {
                petriController = petri;
            }
            else
            {
                UpdatePetriReferenceRPC(petri.NetworkObject);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void UpdatePetriReferenceRPC(NetworkObjectReference petriReference)
        {
            if (!petriReference.TryGet(out NetworkObject netObj)) return;
            
            var petri = netObj.GetComponent<PetriController>();
            petriController = petri;
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
        
        // Sync detaching
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
        
        // Sync attaching
        [Rpc(SendTo.NotMe)]
        private void AttachInOthersRPC(NetworkObjectReference petriReference)
        {
            if (socket.hasSelection)
            {
                Debug.Log("[LidPetri]: I can't attach a petri in my socket, there's one there already");
                return;
            }
            if (petriReference.TryGet(out NetworkObject obj))
            {
                var grabComponent = obj.GetComponent<XRGrabInteractable>();
                if (grabComponent is null)
                {
                    Debug.LogError("Unable to get GrabComponent");
                    return;
                }
                socket.interactionManager.SelectEnter(socket, (IXRSelectInteractable) grabComponent);
                return;
            }
            Debug.LogError("Unable to get NetworkObject");
        }
        
        // Sync positions
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
        [Rpc(SendTo.Owner)]
        private void SynchronizeTransformsPetriSocketRPC()
        {
            petriController.transform.position = socket.attachTransform.position;
            petriController.transform.rotation = socket.attachTransform.rotation;
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
                _rBody.linearVelocity = Vector3.zero;
                _rBody.angularVelocity = Vector3.zero;
                _rBody.isKinematic = false;
            }
            else
            {
                StopRigidBodyRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void StopRigidBodyRPC()
        {
            _rBody.linearVelocity = Vector3.zero;
            _rBody.angularVelocity = Vector3.zero;
            _rBody.isKinematic = false;
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
            ResetSocketRPC();
        }
        [Rpc(SendTo.Owner)]
        private void ResetSocketRPC()
        {
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
