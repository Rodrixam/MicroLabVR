using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class TubeController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private XRGrabInteractable _grabInteractable;
        [SerializeField] private CottonLidController cottonLid;
        [SerializeField] private TubeTimerController tubeTimer;
        [SerializeField] private TubePickSampleTask pickTask;
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private AudioSource timePassingSound;
        public TubeTimerController TubeTimer => tubeTimer;

        [Header("Colliders")]
        [SerializeField] private Collider longCollider;
        [SerializeField] private Collider shortCollider;
        
        private readonly NetworkVariable<bool> _onHeat = 
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        // Initial state references
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Vector3 _initialScale;
        
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

        private void Awake()
        {
            _grabInteractable.selectEntered.AddListener(Grab);
        }
        public override void OnDestroy()
        {
            _grabInteractable.selectEntered.RemoveListener(Grab);
            base.OnDestroy();
        }

        private void Start()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _initialScale = transform.localScale;
        }

        private void Update()
        {
            if (_isOnline && !IsOwner) return;
            
            // Boil Timer
            if (_onHeat.Value && TubeTimer.interval < 3)
            {
                if (!timePassingSound.isPlaying)
                {
                    SetTimePassingSoundSynced(true);  
                } 
                
                TubeTimer.AddBoilTimeSynced(Time.deltaTime);
            }
            else if (timePassingSound.isPlaying)
            {
                SetTimePassingSoundSynced(false);  
            } 
            
            // Sample Pick
            if (cottonLid.isOpen && cottonLid.isFlamed && !pickTask.tracking)
            {
                particles.Play();
                pickTask.ActivateTracking();
            }
            else if(!(cottonLid.isOpen && cottonLid.isFlamed) && pickTask.tracking)
            {
                pickTask.DeactivateTracking();
            }
        }

        private void SetTimePassingSoundSynced(bool state)
        {
            if (!_isOnline)
            {
                if (state)
                {
                    timePassingSound.Play();
                }
                else
                {
                    timePassingSound.Stop();
                }
            }
            else
            {
                SetTimePassingSoundRPC(state);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetTimePassingSoundRPC(bool state)
        {
            if (state)
            {
                timePassingSound.Play();
            }
            else
            {
                timePassingSound.Stop();
            }
        }

        // INTERACTION METHODS
        
        private void Grab(SelectEnterEventArgs args)
        {
            // Skip if wasn't picked by hand
            if (!IsPickedByHand(args)) return;
            
            // Ownership
            if(_isOnline) RequestOwnershipServerRPC();
            
            // Cotton Lid colliders
            //if (cottonLid.isOpen) return;
            SetCottonLidCollidersStateSynced(true);
        }
        [ServerRpc(RequireOwnership = false)]
        private void RequestOwnershipServerRPC(ServerRpcParams rpcParams = default)
        {
            // Change ownership
            ulong grabbingClientId = rpcParams.Receive.SenderClientId;
            GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);
        }

        public void Drop()
        {
            // Don't change colliders if the lid is open or this is not owner
            if (!cottonLid.isOpen || (_isOnline && !IsOwner)) return;
            
            // Cotton Lid colliders
            SetCottonLidCollidersStateSynced(false);
        }

        private bool IsPickedByHand(SelectEnterEventArgs args)
        {
            if (args.interactorObject.transform.CompareTag("LeftHand") ||
                args.interactorObject.transform.CompareTag("RightHand"))
            {
                return true;
            }
            return false;
        }

        // SOCKET METHODS
        
        public void SetColliderStateSynced(bool longState, bool shortState)
        {
            if (!_isOnline)
            {
                longCollider.enabled = longState;
                shortCollider.enabled = shortState;   
            }
            else
            {
                SwapColliderStateRPC(longState, shortState);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SwapColliderStateRPC(bool longState, bool shortState)
        {
            longCollider.enabled = longState;
            shortCollider.enabled = shortState;
        }
        
        /*
        public void SetClockActiveStateSynced(bool state)
        {
            if (!_isOnline)
            {
                tubeTimer.SetVisualsActiveStateSynced(state);   
            }
            else
            {
                SetClockActiveStateRPC(state);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetClockActiveStateRPC(bool state)
        {
            Debug.Log($"[TubeTimer]: Am I setting the clock active? {state}");
            tubeTimer.SetVisualsActiveStateSynced(state);
        }//*/
        
        
        // RPC METHODS
        private void SetCottonLidCollidersStateSynced(bool state)
        {
            if (!_isOnline)
            {
                cottonLid.SetActiveCollidersStateSynced(state);
            }
            else
            {
                SetCottonLidCollidersStateRPC(state);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetCottonLidCollidersStateRPC(bool state)
        {
            cottonLid.SetActiveCollidersStateSynced(state);
        }

        public void SetOnFireSynced(bool state)
        {
            if (!_isOnline)
            {
                _onHeat.Value = state;
            }
            else
            {
                SetOnFireRPC(state);
            }
        }
        [Rpc(SendTo.Owner)]
        private void SetOnFireRPC(bool state)
        {
            _onHeat.Value = state;
        }
        
        
        // ------------------- RESTORATION -------------------
        
        public void RestoreToStartConditions()
        {
            // Physics
            SetColliderStateSynced(true, false);
            StopRigidBodySynced();
            
            // Transform
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            transform.localScale = _initialScale;
            
            // Controller Variables
            ResetNetworkVariablesSynced();
            
            // Tasks
            tubeTimer.RestoreToStartConditionsSynced();
            pickTask.RestoreToStartConditionsSynced();
            cottonLid.RestoreToStartConditions();
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

        // Network Variables
        private void ResetNetworkVariablesSynced()
        {
            if (!_isOnline)
            {
                _onHeat.Value = false;
            }
            else
            {
                ResetNetworkVariablesRPC();
            }
        }
        [Rpc(SendTo.Owner)]
        private void ResetNetworkVariablesRPC()
        {
            _onHeat.Value = false;
        }
    }
}