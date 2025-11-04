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
        public TubeTimerController TubeTimer => tubeTimer;

        [Header("Colliders")]
        [SerializeField] private Collider longCollider;
        [SerializeField] private Collider shortCollider;

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

        private void Start()
        {
            // The clock starts deactivated. It gets active when boiling.
            SetClockActive(false);
            
            pickTask.DeactivateTracking();

            _grabInteractable.selectEntered.AddListener(Grab);
        }

        private void Update()
        {
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

        // INTERACTION METHODS
        
        // this one gets added at start
        private void Grab(SelectEnterEventArgs args)
        {
            // Don't change colliders if lid is open or if it wasn't grabbed by a hand
            if (cottonLid.isOpen || !IsPickedByHand(args)) return;
            
            if (!_isOnline)
            {
                cottonLid.SetActiveCollidersState(true);   
            }
            else
            {
                SetCottonLidCollidersStateRPC(true);
            }
        }

        public void Drop()
        {
            // Don't change colliders if lid is open
            if (cottonLid.isOpen) return;
            
            if (!_isOnline)
            {
                cottonLid.SetActiveCollidersState(false);
            }
            else
            {
                SetCottonLidCollidersStateRPC(false);
            }
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

        // SOCKET METHODS, socket events sync automatically
        
        public void ReduceCollider()
        {
            longCollider.enabled = false;
            shortCollider.enabled = true;
        }
        public void EnlargeColliders()
        {
            longCollider.enabled = true;
            shortCollider.enabled = false;
        }
        public void SetClockActive(bool state)
        {
            tubeTimer.gameObject.SetActive(state);
        }
        
        
        // RPC METHODS
        [Rpc(SendTo.ClientsAndHost)]
        private void SetCottonLidCollidersStateRPC(bool state)
        {
            cottonLid.SetActiveCollidersState(state);
        }
    }

}