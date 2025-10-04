using System.Collections.Generic;
using Scripts;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class BeakGlassController : NetworkBehaviour
    {
        [SerializeField] private List<XRSocketInteractor> sockets;
        [SerializeField] private ParticleSystem bubbles;

        [SerializeField] private float timeToBoil;
    
        private readonly NetworkVariable<float> _timeCounter = 
            new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
        private readonly NetworkVariable<bool> _onFire = 
            new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _boiling = 
            new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private readonly List<TubeController> _tubes = new();
        private bool _isOnline;

        private void Awake()
        {
            foreach (var sock in sockets)
            {
                // Subscribe to socket events
                sock.selectEntered.AddListener(OnObjectPlaced);
                sock.selectExited.AddListener(OnObjectRemoved);   
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var sock in sockets)
            {
                // Always unsubscribe to avoid leaks
                sock.selectEntered.RemoveListener(OnObjectPlaced);
                sock.selectExited.RemoveListener(OnObjectRemoved);
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
            if (_isOnline && !IsOwner) return;
        
            if (_onFire.Value && _timeCounter.Value <= timeToBoil)
            {
                _timeCounter.Value += Time.deltaTime;

                if (_timeCounter.Value > timeToBoil)
                {
                    _boiling.Value = true;

                    if (_isOnline)
                    {
                        ToggleParticlesRPC(true);
                    }
                    else
                    {
                        bubbles.Play();   
                    }
                }
            }

            if (!_onFire.Value && _timeCounter.Value > 0)
            {
                _timeCounter.Value -= Time.deltaTime;
                if (_timeCounter.Value < 0)
                {
                    _timeCounter.Value = 0;
                }
            }
        
            if (_boiling.Value)
            {
                if (!_onFire.Value)
                {
                    _boiling.Value = false;
                
                    if (_isOnline)
                    {
                        ToggleParticlesRPC(true);
                    }
                    else
                    {
                        bubbles.Stop();   
                    }
                    return;
                }

                foreach (var tube in _tubes)
                {
                    tube.AddBoilTime(Time.deltaTime);
                }
            }
        }

        private void OnObjectPlaced(SelectEnterEventArgs args)
        {
            TubeController tube = args.interactableObject.transform.GetComponentInChildren<TubeController>();
            tube.ReduceCollider();
            tube.SetClockActive(true);
            
            _tubes.Add(tube);
        }

        private void OnObjectRemoved(SelectExitEventArgs args)
        {
            TubeController tube = args.interactableObject.transform.GetComponentInChildren<TubeController>();
            tube.EnlargeColliders();
            tube.SetClockActive(false);
        
            _tubes.Remove(tube);
        }

        public void SetOnFire(bool value)
        {
            if (_isOnline && !IsOwner) return;
        
            _onFire.Value = value;
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
