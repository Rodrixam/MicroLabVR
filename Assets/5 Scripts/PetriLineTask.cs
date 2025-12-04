using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    [RequireComponent(typeof(LidPetriController))]
    public class PetriLineTask : NetworkBehaviour
    {
        // REFERENCES
        
        private Transform _penTip;
        private LidPetriController _lidController;
        
        [SerializeField] private CollisionReporter[] reporters;
        
        [SerializeField] private GameObject divisionLines;
        
        [SerializeField] private GameObject[] interactivePoints;
        [SerializeField] private LineRenderer[] lineRenderers;
        
        [SerializeField] private ParticleSystem particles;

        [Header("Debug")]
        [SerializeField] private InputActionReference skipButton;
        public bool skipButtonEnabled;
        
        // STATE VARIABLES
        
        private bool _isOnline;
        
        private readonly NetworkVariable<int> _netLinesProgress = 
            new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        private readonly NetworkVariable<bool> _netFinishingLine = 
            new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
        private readonly NetworkVariable<bool> _allLinesDrawn = 
            new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public bool allLinesDrawn => _allLinesDrawn.Value;
        
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
            _allLinesDrawn.OnValueChanged += ChangeDivisionLinesActive;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _isOnline = false;
            _allLinesDrawn.OnValueChanged -= ChangeDivisionLinesActive;
        }
        
        private void OnEnable()
        {
            _lidController = GetComponent<LidPetriController>();
            
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
            divisionLines.SetActive(_allLinesDrawn.Value);

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
            
            skipButton.action.performed += SkipTask;
            skipButton.action.Enable();
        }

        private void OnDisable()
        {
            skipButton.action.performed -= SkipTask;
            skipButton.action.Disable();
        }

        private void Update()
        {
            if (_allLinesDrawn.Value) return;
            
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
            if (_lidController.isGrabbed && _netFinishingLine.Value && (_penTip is not null || !IsOwner) 
                && lineRenderers[_netLinesProgress.Value].positionCount > 1)
            {
                lineRenderers[_netLinesProgress.Value].SetPosition(0, _netInteractivePointsPosition[_netLinesProgress.Value + 1]);
                lineRenderers[_netLinesProgress.Value].SetPosition(1, _netPenTipPosition.Value);
            }
        }
        
        // INTERACTION METHODS
        
        public void Grab()
        {
            if (_allLinesDrawn.Value) return;

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
            if (_allLinesDrawn.Value) return;
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
            else
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
        }

        private void PointReached(Collider other)
        {
            if (!other.CompareTag("PenTip") || !_lidController.isGrabbed || _allLinesDrawn.Value || (_isOnline && !IsOwner)) return;
            Debug.Log("[PETRI LINE TASK]: Point reached valid.");
        
            // Set penTip transform if null
            if (_penTip == null)
            {
                _penTip = other.transform;
            }
            
            // Beginning line draw
            if (!_netFinishingLine.Value)
            {
                _netFinishingLine.Value = true;
                    
                // Activate center point
                RequestInteractivePointActiveChangeSynced(_netLinesProgress.Value + 1, false);
                RequestInteractivePointActiveChangeSynced(0, true);
                    
                // Activate line renderer
                RequestLineRendererPositionCountSynced(_netLinesProgress.Value, 2);
            }
            // Finishing line draw
            else
            {
                _netLinesProgress.Value++;
                _netFinishingLine.Value = false;
                
                // Complete drawing
                if (_netLinesProgress.Value > 2)
                {
                    CompleteDrawingSynced();
                    return;
                }
                
                // Activate next point
                RequestInteractivePointActiveChangeSynced(_netLinesProgress.Value + 1, true);
                RequestInteractivePointActiveChangeSynced(0, false);
            }
        }

        private void PenLifted()
        {
            if (_allLinesDrawn.Value) return;
            
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

        private void RequestInteractivePointActiveChangeSynced(int index, bool value)
        {
            if (!_isOnline)
            {
                interactivePoints[index].SetActive(value);
            }
            else
            {
                RequestInteractivePointActiveChangeRPC(index, value);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void RequestInteractivePointActiveChangeRPC(int index, bool value)
        {
            interactivePoints[index].SetActive(value);
        }

        private void RequestLineRendererPositionCountSynced(int index, int value)
        {
            if (!_isOnline)
            {
                lineRenderers[index].positionCount = value;

                if (value < 2) return;
                lineRenderers[index].SetPosition(0, _netInteractivePointsPosition[index + 1]);
                lineRenderers[index].SetPosition(1, _netInteractivePointsPosition[index + 1]);
            }
            else
            {
                RequestLineRendererPositionCountRPC(index, value);
            }
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

        private void CompleteDrawingSynced()
        {
            // Return if not called from the owner
            if(_isOnline && !IsOwner)
            {
                Debug.LogError("[PetriLineTask]: CompleteDrawingSynced called from not owner");
                return;
            }
            
            _allLinesDrawn.Value = true;
            
            // Activate final draw
            divisionLines.SetActive(true);
            
            // Deactivate interactive points
            for (int i = 0; i < interactivePoints.Length; i++) {
                RequestInteractivePointActiveChangeSynced(i, false);
            }
            
            // Deactivate line renderers
            for (int i = 0; i < lineRenderers.Length; i++)
            {
                RequestLineRendererPositionCountSynced(i, 0);
            }
            
            // Victory particles
            if (!_isOnline)
            {
                particles.Play();
            }
            else
            {
                RequestParticleSystemPlayRPC();
            }
            
            // Update Petri Controller
            _lidController.PetriController.CompleteTaskSynced();
        }

        private void SkipTask(InputAction.CallbackContext context)
        {
            if (!skipButtonEnabled) return;
            CompleteDrawingSynced();
        }
        
        
        // ------------------- RESTORATION -------------------
        
        
        public void RestoreToStartConditions()
        {
            // Controller Variables
            ResetComponentsSynced();
            ResetNetworkVariablesSynced();
        }
        
        
        // Network Variables
        private void ResetNetworkVariablesSynced()
        {
            if (!_isOnline)
            {
                _netLinesProgress.Value = 0;
                _netFinishingLine.Value = false;
                _allLinesDrawn.Value = false;
                
                for (var i = 0; i < interactivePoints.Length; i++)
                {
                    _netInteractivePointsPosition[i] = Vector3.zero;
                }

                _netPenTipPosition.Value = default;
            }
            else
            {
                ResetNetworkVariablesRPC();
            }
        }
        [Rpc(SendTo.Owner)]
        private void ResetNetworkVariablesRPC()
        {
            _netLinesProgress.Value = 0;
            _netFinishingLine.Value = false;
            _allLinesDrawn.Value = false;
            
            for (var i = 0; i < interactivePoints.Length; i++)
            {
                _netInteractivePointsPosition[i] = Vector3.zero;
            }

            _netPenTipPosition.Value = default;
        }
        
        
        // Components
        private void ResetComponentsSynced()
        {
            if (!_isOnline)
            {
                enabled = true;
                _penTip = null;
                divisionLines.SetActive(false);
                
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
            else
            {
                ResetComponentsRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void ResetComponentsRPC()
        {
            enabled = true;
            _penTip = null;
            
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
    }
}
