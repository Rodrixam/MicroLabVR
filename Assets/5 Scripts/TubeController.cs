using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class TubeController : NetworkBehaviour
    {
        [SerializeField] private CottonLidController cottonLid;
        
        [SerializeField] private TubeTimerController tubeTimer;
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
            SetClockActive(false);
            cottonLid.SetActiveCollidersState(false);
        }

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

        public void Grab()
        {
            cottonLid.SetActiveCollidersState(true);
        }

        public void Drop()
        {
            cottonLid.SetActiveCollidersState(false);
        }

        public void SetClockActive(bool state)
        {
            tubeTimer.gameObject.SetActive(state);
        }
    }
}