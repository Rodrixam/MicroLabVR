using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class BeakGlassController : NetworkBehaviour
    {
        // References
        [SerializeField] private List<XRSocketInteractor> sockets;
        [SerializeField] private ParticleSystem bubbles;

        private readonly List<TubeController> _tubes = new();
        
        // Parameters
        [SerializeField] private float timeToBoil;
    
        // Variables
        private readonly NetworkVariable<float> _timeCounter = 
            new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _onFire = 
            new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _boiling = 
            new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private bool _isOnline;

        private void Awake()
        {
            foreach (var sock in sockets)
            {
                // Subscribe to socket events
                // they get synchronized automatically
                sock.selectEntered.AddListener(OnTubePlaced);
                sock.selectExited.AddListener(OnTubeRemoved);   
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var sock in sockets)
            {
                // Always unsubscribe to avoid leaks
                sock.selectEntered.RemoveListener(OnTubePlaced);
                sock.selectExited.RemoveListener(OnTubeRemoved);
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

    
        private void Update()
        {
            // Only owner updates
            if (_isOnline && !IsOwner) return;
        
            // Heating
            if (_onFire.Value && _timeCounter.Value <= timeToBoil)
            {
                _timeCounter.Value += Time.deltaTime;

                // Boiling time reached
                if (_timeCounter.Value > timeToBoil)
                {
                    _boiling.Value = true;
                    ToggleParticles(true);
                }
            }

            // Cooling
            if (!_onFire.Value && _timeCounter.Value > 0)
            {
                _timeCounter.Value -= Time.deltaTime;
                
                if (_timeCounter.Value < 0)
                {
                    _timeCounter.Value = 0;
                }
            }
        
            // Interactions while boiling
            if (_boiling.Value)
            {
                // Turn off
                if (!_onFire.Value)
                {
                    _boiling.Value = false;
                    ToggleParticles(false);
                    return;
                }

                // Heating tubes
                foreach (var tube in _tubes)
                {
                    tube.TubeTimer.AddBoilTime(Time.deltaTime);
                }
            }
        }
        
        // SOCKET EVENTS

        private void OnTubePlaced(SelectEnterEventArgs args)
        {
            TubeController tube = args.interactableObject.transform.GetComponentInChildren<TubeController>();
            tube.ReduceCollider();
            tube.SetClockActive(true);
            
            _tubes.Add(tube);
        }

        private void OnTubeRemoved(SelectExitEventArgs args)
        {
            TubeController tube = args.interactableObject.transform.GetComponentInChildren<TubeController>();
            tube.EnlargeColliders();
            tube.SetClockActive(false);
        
            _tubes.Remove(tube);
        }

        // ON FIRE ACCESS METHODS
        
        public void SetOnFireState(bool value)
        {
            if (!_isOnline)
            {
                _onFire.Value = value;
            }
            else
            {
                SetOnFireStateRPC(value);
            }
        }
        
        [Rpc(SendTo.Owner)]
        private void SetOnFireStateRPC(bool value)
        {
            _onFire.Value = value;
        }

        // PARTICLES METHODS
        
        private void ToggleParticles(bool value)
        {
            if (!_isOnline)
            {
                if (value)
                {
                    bubbles.Play();
                }
                else
                {
                    bubbles.Stop();      
                }
            }
            else
            {
                ToggleParticlesRPC(value);
            }
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        private void ToggleParticlesRPC(bool value)
        {
            if (value)
            {
                bubbles.Play();
            }
            else
            {
                bubbles.Stop();
            }
        }
    }
}
