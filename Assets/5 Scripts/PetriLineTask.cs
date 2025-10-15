using System;
using Scripts;
using Unity.Netcode;
using UnityEngine;


// ReSharper disable once CheckNamespace
namespace Scripts
{
    [RequireComponent(typeof(PetriController))]
    public class PetriLineTask : NetworkBehaviour
    {
        // REFERENCES
        
        private Transform _penTip;
        private PetriController _controller;
        
        [SerializeField] private GameObject divisionLines;
        
        [SerializeField] private GameObject[] interactivePoints;
        
        [SerializeField] private LineRenderer[] lineRenderers;
        
        [SerializeField] private ParticleSystem particles;
        
        [SerializeField] CollisionReporter[] reporters;
        
        // STATE VARIABLES
        
        private bool _isOnline;
        
        private readonly NetworkVariable<int> _netLinesProgress = 
            new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        private readonly NetworkVariable<bool> _netFinishingLine = 
            new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
        private readonly NetworkVariable<bool> _netAllLinesDrawn = 
            new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        // NETWORKED POSITIONS

        private readonly NetworkList<Vector3> _netInteractivePointsPosition = new( 
            new[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero } , 
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private readonly NetworkVariable<Vector3> _netPenTipPosition = new(default,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _isOnline = true;
            _netAllLinesDrawn.OnValueChanged += ChangeDivisionLinesActive;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _isOnline = false;
            _netAllLinesDrawn.OnValueChanged -= ChangeDivisionLinesActive;
        }
        
        private void OnEnable()
        {
            _controller = GetComponent<PetriController>();
            
            // Set trigger collision events
            foreach (var reporter in reporters)
            {
                reporter.OnTriggerEntered += (other) =>
                {
                    if (!reporter.CompareTag("InteractivePoint")) return;
                    PointReached(other);
                };
            
                reporter.OnTriggerExited += (_) =>
                {
                    if (!reporter.CompareTag("LimitZone")) return;
                    PenLifted();
                };
            }
            
            // Deactivate completed image
            divisionLines.SetActive(_netAllLinesDrawn.Value);

            // Deactivate interactive points
            foreach (var t in interactivePoints)
            {
                t.SetActive(false);
            }
        
            // Deactivate line renderers
            foreach (var lr in lineRenderers)
            {
                lr.positionCount = 0;
            }
        }

        private void Update()
        {
            if (_netAllLinesDrawn.Value) return;
            
            // Update interactive points positions from the owner
            // (gravity moves the object between syncs otherwise)
            if (!_isOnline || IsOwner)
            {
                for (var i = 0; i < interactivePoints.Length; i++)
                {
                    _netInteractivePointsPosition[i] = interactivePoints[i].transform.position;
                }

                if (_penTip is not null)
                {
                    _netPenTipPosition.Value = _penTip.position;
                }
            }
            
            // Update line renderers for complete lines
            for (var i = 0; i < _netLinesProgress.Value; i++)
            {
                if (i > 2) break;
                lineRenderers[i].SetPosition(0, _netInteractivePointsPosition[i + 1]);
                lineRenderers[i].SetPosition(1, _netInteractivePointsPosition[0]);
            }
                
            // Update line renderer following pen tip
            if (_controller.IsGrabbed() && _netFinishingLine.Value && (_penTip is not null || !IsOwner) 
                && lineRenderers[_netLinesProgress.Value].positionCount > 1)
            {
                lineRenderers[_netLinesProgress.Value].SetPosition(0, _netInteractivePointsPosition[_netLinesProgress.Value + 1]);
                lineRenderers[_netLinesProgress.Value].SetPosition(1, _netPenTipPosition.Value);
            }
        }
        
        // INTERACTION METHODS
        
        public void Grab()
        {
            if (_netAllLinesDrawn.Value) return;

            // ------------------------- LOCAL -------------------------
            if (!_isOnline)
            {
                // Activate interactive point
                interactivePoints[_netLinesProgress.Value + 1].SetActive(true); 
            }
            // ------------------------- ONLINE -------------------------
            else
            {
                RequestInteractivePointActiveChangeRPC(_netLinesProgress.Value + 1, true);
            }
        }
        
        public void Drop()
        {
            if (_netAllLinesDrawn.Value) return;
            _penTip = null;

            // ------------------------- LOCAL -------------------------
            if (!_isOnline)
            {
                // Deactivate interactive points
                foreach (var p in interactivePoints)
                {
                    p.SetActive(false);
                }
                
                // Deactivate uncompleted line renderers
                for(var i = _netLinesProgress.Value; i < lineRenderers.Length; i++)
                {
                    lineRenderers[i].positionCount = 0;
                }
            }
            // ------------------------- ONLINE -------------------------
            else if(IsOwner)
            {
                // Deactivate interactive points
                for(var i = 0; i < interactivePoints.Length; i++)
                {
                    RequestInteractivePointActiveChangeRPC(i, false);
                }
                
                // Deactivate uncompleted line renderers
                for(var i = _netLinesProgress.Value; i < lineRenderers.Length; i++)
                {
                    RequestLineRendererPositionCountRPC(i, 0);
                }
            }
            else
            {
                Debug.LogError("Petri dropped by not owner... a fix for delay in ownership assignation might be needed");
            }
        }

        private void PointReached(Collider other)
        {
            if (!other.CompareTag("PenTip") || !_controller.IsGrabbed() || _netAllLinesDrawn.Value || (_isOnline && !IsOwner)) return;
        
            // Set penTip transform if null
            if (_penTip == null)
            {
                _penTip = other.transform;
            }
            
            // ------------------------- LOCAL -------------------------
            if (!_isOnline)
            {
                // Beginning line draw
                if (!_netFinishingLine.Value)
                {
                    _netFinishingLine.Value = true;
                    
                    // Activate center point
                    interactivePoints[_netLinesProgress.Value + 1].SetActive(false);
                    interactivePoints[0].SetActive(true);

                    // Activate line renderer
                    lineRenderers[_netLinesProgress.Value].positionCount = 2;
                        
                    lineRenderers[_netLinesProgress.Value].SetPosition(0,
                        _netInteractivePointsPosition[_netLinesProgress.Value + 1]);
                    lineRenderers[_netLinesProgress.Value].SetPosition(1,
                        _netInteractivePointsPosition[_netLinesProgress.Value + 1]);
                }
                // Finishing line draw
                else
                {
                    _netLinesProgress.Value++;
                    _netFinishingLine.Value = false;
                
                    // Complete drawing
                    if (_netLinesProgress.Value > 2)
                    {
                        _netAllLinesDrawn.Value = true;
                        
                        // Activate final draw
                        divisionLines.SetActive(true);
                        
                        // Deactivate interactive points
                        foreach (var p in interactivePoints)
                        {
                            p.SetActive(false);
                        }
                    
                        // Deactivate line renderers
                        foreach (var lr in lineRenderers)
                        {
                            lr.positionCount = 0;
                        }
                        
                        // Victory particles
                        particles.Play();
                        
                        // Update Petri Controller
                        _controller.CompleteActivity();
                        return;
                    }
                
                    // Activate next point
                    interactivePoints[_netLinesProgress.Value + 1].SetActive(true);
                    interactivePoints[0].SetActive(false);
                }
            }
            // ------------------------- ONLINE -------------------------
            else
            {
                // Beginning line draw
                if (!_netFinishingLine.Value)
                {
                    _netFinishingLine.Value = true;
                    
                    // Activate center point
                    RequestInteractivePointActiveChangeRPC(_netLinesProgress.Value + 1, false);
                    RequestInteractivePointActiveChangeRPC(0, true);
                    
                    // Activate line renderer
                    RequestLineRendererPositionCountRPC(_netLinesProgress.Value, 2);
                }
                // Finishing line draw
                else
                {
                    _netLinesProgress.Value++;
                    _netFinishingLine.Value = false;
                
                    // Complete drawing
                    if (_netLinesProgress.Value > 2)
                    {
                        _netAllLinesDrawn.Value = true;
                        
                        // Deactivate interactive points
                        for (int i = 0; i < interactivePoints.Length; i++) {
                            RequestInteractivePointActiveChangeRPC(i, false);
                        }
                        
                        // Deactivate line renderers
                        for (int i = 0; i < lineRenderers.Length; i++)
                        {
                            RequestLineRendererPositionCountRPC(i, 0);
                        }
                        
                        // Victory particles
                        RequestParticleSystemPlayRPC();
                        return;
                    }
                
                    // Activate next point
                    RequestInteractivePointActiveChangeRPC(_netLinesProgress.Value + 1, true);
                    RequestInteractivePointActiveChangeRPC(0, false);
                }
            }

        }

        private void PenLifted()
        {
            if (_netAllLinesDrawn.Value) return;
            
            _penTip = null;

            if (_isOnline && !IsOwner) return;
            Debug.Log("[OWNER]: Pen tip removed from zone");
            
            _netFinishingLine.Value = false;
            
            // ------------------------- LOCAL -------------------------
            if (!_isOnline)
            {
                // Deactivate center point (just in case)
                interactivePoints[_netLinesProgress.Value + 1].SetActive(true);
                interactivePoints[0].SetActive(false);
        
                // Deactivate line renderer
                lineRenderers[_netLinesProgress.Value].positionCount = 0;
            }
            // ------------------------- ONLINE -------------------------
            else
            {
                // Deactivate center point (just in case)
                RequestInteractivePointActiveChangeRPC(_netLinesProgress.Value + 1, true);
                RequestInteractivePointActiveChangeRPC(0, false);
        
                // Deactivate line renderer
                RequestLineRendererPositionCountRPC(_netLinesProgress.Value, 0);
            }
        }

        // RPC METHODS
        
        [Rpc(SendTo.ClientsAndHost)]
        private void RequestParticleSystemPlayRPC()
        {
            particles.Play();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void RequestInteractivePointActiveChangeRPC(int index, bool value)
        {
            interactivePoints[index].SetActive(value);
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        private void RequestLineRendererPositionCountRPC(int index, int value)
        {
            lineRenderers[index].positionCount = value;
        }
        
        // CHANGE METHODS
        private void ChangeDivisionLinesActive(bool previous, bool current)
        {
            divisionLines.SetActive(current);
        }
    }
}
