using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class TubePickSampleTask : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private TubeController controller;
    
        [Header("Colliders")] 
        [SerializeField] private CollisionReporter startPoint;
        [SerializeField] private CollisionReporter triggerZone;
        [SerializeField] private CollisionReporter pickupPoint;

        [Header("Values")]
        [SerializeField] private Samples sample;
        [SerializeField] private Material sampleMaterial;

        public bool tracking { get; private set; }
    
        private readonly List<AsaController> _asaTracking = new();
    
        
        void Start()
        {
            // Set trigger collision events
            startPoint.OnTriggerEntered += (other) =>
            {
                if (!tracking || !other.CompareTag("AsaTip")) return;

                var asa = other.GetComponentInParent<AsaController>();
                if (asa is not null && asa.isSterilized)
                {
                    _asaTracking.Add(asa);   
                    asa.StartMicrobeParticlesRPC();
                }
            };
            
            triggerZone.OnTriggerExited += (other) =>
            {
                if (!tracking || !other.CompareTag("AsaTip")) return;

                var asa = other.GetComponentInParent<AsaController>();
                if (_asaTracking.Remove(asa))
                {
                    asa.StopMicrobeParticlesRPC();
                }
            };
            triggerZone.OnTriggerStayed += (other) =>
            {
                foreach (var asa in _asaTracking)
                {
                    float distance = Vector3.Cross(triggerZone.transform.up, other.transform.position - triggerZone.transform.position).magnitude;
                    float normalized = Mathf.InverseLerp(0f, 0.01f, distance);
                
                    asa.SetMicrobeParticlesColorRPC(Color.Lerp(Color.green, Color.red, normalized));
                }
            };
        
            pickupPoint.OnTriggerEntered += (other) =>
            {
                if (!tracking || !other.CompareTag("AsaTip")) return;

                var asa = other.GetComponentInParent<AsaController>();
                if (asa is not null && _asaTracking.Contains(asa))
                {
                    asa.AttachMicrobe(sample, controller.TubeTimer.interval, sampleMaterial);
                }
            };
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
        
        // SAMPLES ENUM
    
        public enum Samples
        {
            None, EColi, SAureus, Spores
        }
    }
}
