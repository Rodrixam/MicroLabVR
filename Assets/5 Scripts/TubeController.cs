using Unity.Netcode;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class TubeController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private CottonLidController cottonLid;
        [SerializeField] private TubeTimerController tubeTimer;
        [SerializeField] private TubePickSampleTask pickTask;
        [SerializeField] private ParticleSystem particles;
        public TubeTimerController TubeTimer => tubeTimer;

        [Header("Colliders")]
        [SerializeField] private Collider longCollider;
        [SerializeField] private Collider shortCollider;

        private bool _isOnline;
        
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
        
        public void Grab()
        {
            if (!cottonLid.isOpen)
            {
                if (!_isOnline)
                {
                    cottonLid.SetActiveCollidersState(true);   
                }
                else
                {
                    SetCottonLidCollidersStateRPC(true);
                }
            }
        }

        public void Drop()
        {
            if (!cottonLid.isOpen)
            {
                if (!_isOnline)
                {
                    cottonLid.SetActiveCollidersState(false);   
                }
                else
                {
                    SetCottonLidCollidersStateRPC(false);
                }
            }
        }

        // Meant for use in BeakGlass sockets
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