using System.Collections.Generic;
using Scripts;
using UnityEngine;

public class TubePickSampleTask : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CollisionReporter startPoint;
    [SerializeField] private CollisionReporter triggerZone;
    [SerializeField] private CollisionReporter pickupPoint;

    [Header("Values")]
    [SerializeField] private Samples sample;
    [SerializeField] private Material sampleMaterial;

    private bool _tracking = false;
    public bool tracking => _tracking;

    private List<AsaController> _asaTracking = new();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Set trigger collision events
        startPoint.OnTriggerEntered += (other) =>
        {
            if (!_tracking || !other.CompareTag("AsaTip")) return;

            var asa = other.GetComponentInParent<AsaController>();
            if (asa is not null && asa.isSterilized)
            {
                _asaTracking.Add(asa);   
            }
        };
            
        triggerZone.OnTriggerExited += (other) =>
        {
            if (!_tracking || !other.CompareTag("AsaTip")) return;

            _asaTracking.Remove(other.GetComponentInParent<AsaController>());
        };
        
        pickupPoint.OnTriggerEntered += (other) =>
        {
            if (!_tracking || !other.CompareTag("AsaTip")) return;

            var asa = other.GetComponentInParent<AsaController>();
            if (asa is not null && _asaTracking.Contains(asa))
            {
                asa.AttachMicrobe(sample, sampleMaterial);
            }
        };
    }

    public void ActivateTracking()
    {
        _tracking = true;
    }

    public void DeactivateTracking()
    {
        _tracking = false;
        _asaTracking.Clear();
    }
    
    public enum Samples
    {
        None, eColi, sAureus, Spores
    }
}
