using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class TubePickSampleTask : NetworkBehaviour
    {
        [Header("References")] 
        [SerializeField] private TubeController controller;
        [SerializeField] private AudioSource wrongBuzz;
    
        [Header("Colliders")] 
        [SerializeField] private CollisionReporter startPoint;
        [SerializeField] private CollisionReporter triggerZone;
        [SerializeField] private CollisionReporter pickupPoint;

        [Header("Values")]
        [SerializeField] private Samples sample;
        [SerializeField] private Material sampleMaterial;

        public bool tracking { get; private set; }
    
        private readonly List<AsaController> _asaTracking = new();
        private bool _isOnline;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _isOnline = true;
        }
        public override void OnNetworkDespawn()
        {
            _isOnline = false;
            base.OnNetworkDespawn();
        }
        
        void Start()
        {
            // Asa enters start point
            startPoint.OnTriggerEntered += (other) =>
            {
                if (!other.CompareTag("AsaTip") || !controller.CottonLid.isOpen) return;
                if (!tracking)
                {
                    wrongBuzz.Play();
                    return;
                }

                var asa = other.GetComponentInParent<AsaController>();
                if (asa is not null)
                {
                    if (!asa.isSterilized && asa.sample != sample)
                    {
                        wrongBuzz.Play();
                    }
                    
                    _asaTracking.Add(asa);   
                    asa.SetMicrobeParticlesState(true);
                }
            };
            // Asa exits tracking zone
            triggerZone.OnTriggerExited += (other) =>
            {
                if (!tracking || !other.CompareTag("AsaTip")) return;

                var asa = other.GetComponentInParent<AsaController>();
                if (_asaTracking.Remove(asa))
                {
                    asa.SetMicrobeParticlesState(false);
                }
            };
            // Asa stays on tracking zone
            triggerZone.OnTriggerStayed += (other) =>
            {
                foreach (var asa in _asaTracking)
                {
                    float distance = Vector3.Cross(triggerZone.transform.up, other.transform.position - triggerZone.transform.position).magnitude;
                    float normalized = Mathf.InverseLerp(0f, 0.01f, distance);
                
                    asa.SetMicrobeParticlesColorRPC(Color.Lerp(Color.green, Color.red, normalized));
                }
            };
            // Asa gets the sample
            pickupPoint.OnTriggerEntered += (other) =>
            {
                if (!tracking || !other.CompareTag("AsaTip")) return;
                AttachMicrobeToAsaSynced(other);
            };
        }

        // Asa gets the Sample
        private void AttachMicrobeToAsaSynced(Collider other)
        {   
            var asa = other.GetComponentInParent<AsaController>();
            if (asa is not null && _asaTracking.Contains(asa))
            {
                if(!_isOnline)
                {
                    asa.AttachMicrobe(sample, controller.TubeTimer.interval, sampleMaterial);
                }
                else
                {
                    NetworkObjectReference asaRef = asa.GetComponent<NetworkObject>();
                    AttachMicrobeToAsaRPC(asaRef);
                }
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void AttachMicrobeToAsaRPC(NetworkObjectReference asaReference)
        {
            if (asaReference.TryGet(out NetworkObject networkObject))
            {
                var asa = networkObject.GetComponent<AsaController>();
                asa.AttachMicrobe(sample, controller.TubeTimer.interval, sampleMaterial);
            }
        }
        
        // ACCESS METHODS
        // Called through TubeController Update, which is not limited to owner
        // Synced automatically

        public void ActivateTracking()
        {
            tracking = true;
        }

        public void DeactivateTracking()
        {
            tracking = false;
            _asaTracking.Clear();
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void DeactivateTrackingRPC()
        {
            DeactivateTracking();
        }
        
        
        // ------------------- RESTORATION -------------------
        
        public void RestoreToStartConditionsSynced()
        {
            // Components
            ResetComponentsSynced();
        }
        
        // Components
        private void ResetComponentsSynced()
        {
            if (!_isOnline)
            {
                DeactivateTracking();
            }
            else
            {
                DeactivateTrackingRPC();
            }
        }
        
        // SAMPLES ENUM
    
        public enum Samples
        {
            None, EColi, SAureus, Spores
        }
    }
}
