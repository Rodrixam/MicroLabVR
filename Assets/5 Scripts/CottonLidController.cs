using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class CottonLidController : NetworkBehaviour
    {
        // Colliders
        [Header("Colliders")] 
        [SerializeField] private Collider lidCollider;
        [SerializeField] private XRGrabInteractable handler;

        // Visual
        [Header("Visual")]
        [SerializeField] private GameObject warningUI;
        [SerializeField] private MeshRenderer lidRenderer;
        
        // Audio
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip corkClip;
        [SerializeField] private AudioClip uncorkClip;

        private GameController _gameController;
        
        private bool _isOnline;
    
        private readonly NetworkVariable<bool> _isFlamed = 
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _isOpen = 
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        public bool isFlamed => _isFlamed.Value;
        public bool isOpen => _isOpen.Value;

        void Start()
        {
            // Deactivate grab collider
            SetActiveCollidersStateSynced(false);
            
            // Activate feedback
            warningUI.SetActive(false);

            // Get gameController reference
            _gameController = FindAnyObjectByType<GameController>();
            if (_gameController is null)
            {
                Debug.LogError("[COTTON LID]: GameController not found in scene.");
            }
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
        
        // GRAB
        public void Grab()
        {
            if (!_isOnline)
            {
                // Open the lid
                if (!isOpen)
                {
                    _isOpen.Value = true;
                    _isFlamed.Value = false;
                    lidRenderer.enabled = false;
                    warningUI.SetActive(true);
                    AttachBallToHand();
                    PlayLidSoundSynced(true);
                }
                // Close the lid
                else if (DetachBallFromHand())
                {
                    _isOpen.Value = false;
                    lidRenderer.enabled = true;
                    warningUI.SetActive(false);
                    
                    PlayLidSoundSynced(false);
                }
            }
            else
            {
                // Start opening the lid
                if (!isOpen)
                {
                    DetachBallRPC();
                    SetWarningActiveStateRPC(true);
                    AttachBallToHand();
                    PlayLidSoundSynced(true);
                }
                // Close the lid
                else if (DetachBallFromHand())
                {
                    AttachBallRPC();
                    SetWarningActiveStateRPC(false);
                    PlayLidSoundSynced(false);
                }
            }
            ForceRelease();
        }
        [Rpc(SendTo.Owner)]
        private void DetachBallRPC()
        {   
            _isOpen.Value = true;
            _isFlamed.Value = false;
            SetLidRendererRPC(false);
        }
        [Rpc(SendTo.Owner)]
        private void AttachBallRPC()
        {   
            _isOpen.Value = false;
            SetLidRendererRPC(true);
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetLidRendererRPC(bool state)
        {
            lidRenderer.enabled = state;
        }
        private void ForceRelease()
        {
            var interactors = new List<IXRSelectInteractor>(handler.interactorsSelecting);

            // Loop through all interactors currently holding this object
            foreach (var interactor in interactors)
            {
                handler.interactionManager.SelectExit(interactor, handler);
            }
        }
        [ServerRpc(RequireOwnership = false)]
        public void RequestOwnershipServerRPC(ServerRpcParams rpcParams = default)
        {
            // Change ownership
            ulong grabbingClientId = rpcParams.Receive.SenderClientId;
            GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);
            DropRPC();
        }
        // DROP
        
        public void Drop()
        {
            Debug.Log($"[CottonLid]: I'm resetting handler localPosition. IsOwner? {IsOwner}");
            handler.transform.localPosition = Vector3.zero;
            handler.transform.localRotation = Quaternion.identity;
        }

        [Rpc(SendTo.Authority)]
        public void DropRPC()
        {
            Drop();
        }

        private void OnTriggerEnter(Collider other)
        {
            // Flame the tube
            if (other.CompareTag("BurnerFlame") && isOpen && !_isFlamed.Value &&
                other.GetComponentInParent<BunsenController>().onFire)
            {
                _isFlamed.Value = true;

                if (!_isOnline)
                {
                    warningUI.SetActive(false);
                }
                else
                {
                    SetWarningActiveStateRPC(false);
                }
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetWarningActiveStateRPC(bool state)
        {        
            warningUI.SetActive(state);
        }

        // Colliders
        public void SetActiveCollidersStateSynced(bool state)
        {
            Debug.Log($"[CottonLid]: I'm setting my colliders {state}.");
            if (!_isOnline)
            {
                lidCollider.enabled = state;
            }
            else //if(IsOwner)
            {
                SetActiveCollidersStateRPC(state);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetActiveCollidersStateRPC(bool state)
        {
            lidCollider.enabled = state;
        }

        // VISUALS
        private void AttachBallToHand()
        {
            foreach (var interactor in handler.interactorsSelecting)
            {
                if (interactor.transform.CompareTag("LeftHand"))
                {
                    if (_gameController.AttachCottonBallToHand(false)) return;
                }
                else if (interactor.transform.CompareTag("RightHand"))
                {
                    if (_gameController.AttachCottonBallToHand(true)) return;
                }
            }
        }
        private bool DetachBallFromHand()
        {
            foreach (var interactor in handler.interactorsSelecting)
            {
                if (interactor.transform.CompareTag("LeftHand"))
                {
                    if (_gameController.DetachCottonBall(false)) return true;
                }
                else if (interactor.transform.CompareTag("RightHand"))
                {
                    if (_gameController.DetachCottonBall(true)) return true;
                }
            }

            return false;
        }
        
        // AUDIO STUFF
        private void PlayLidSoundSynced(bool open)
        {
            if (!_isOnline)
            {
                if (open)
                {
                    audioSource.clip = uncorkClip;
                    audioSource.Play();
                }
                else
                {
                    audioSource.clip = corkClip;
                    audioSource.Play();
                }
            }
            else
            {
                PlayLidSoundRPC(open);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void PlayLidSoundRPC(bool open)
        {
            if (open)
            {
                audioSource.clip = uncorkClip;
                audioSource.Play();
            }
            else
            {
                audioSource.clip = corkClip;
                audioSource.Play();
            }
        }
        

        // ------------------- RESTORATION -------------------
        
        public void RestoreToStartConditions()
        {
            // Physics
            SetActiveCollidersStateSynced(false);
            
            if (!_isOnline)
            {
                Drop();   
            }
            else
            {
                DropRPC();
            }
            
            // Controller Variables
            ResetNetworkVariablesSynced();
            
            // Components
            ResetComponentsSynced();
        }

        // Network Variables
        private void ResetNetworkVariablesSynced()
        {
            if (!_isOnline)
            {
                _isFlamed.Value = false;
                _isOpen.Value = false;
            }
            else
            {
                ResetNetworkVariablesRPC();
            }
        }
        [Rpc(SendTo.Owner)]
        private void ResetNetworkVariablesRPC()
        {
            _isFlamed.Value = false;
            _isOpen.Value = false;
        }
        
        // Components
        private void ResetComponentsSynced()
        {
            if (!_isOnline)
            {
                warningUI.SetActive(false);
                
                lidRenderer.enabled = true;
                
                _gameController.DetachCottonBall(true);
                _gameController.DetachCottonBall(false);
            }
            else
            {
                ResetComponentsRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void ResetComponentsRPC()
        {
            warningUI.SetActive(false);
            
            lidRenderer.enabled = true;
            
            _gameController.DetachCottonBall(true);
            _gameController.DetachCottonBall(false);
        }
    }
}
