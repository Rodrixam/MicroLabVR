using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

// ReSharper disable Unity.PerformanceCriticalCodeInvocation
// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class ClipboardController : NetworkBehaviour
    {
        // Text references & variables
        [SerializeField] private int checkBoxNum;
        [SerializeField] private TextMeshProUGUI mainTextUGUI;
        
        private const char CBox = '□';
        private const char CCheck = '✔';

        private int[] _checkBoxPlaces;
        private readonly bool[] _checkBoxStates = {false, false, false, false, false, false};
        
        // Audio references
        [SerializeField] private AudioSource audioSource;

        // Collider references
        [SerializeField] private List<CollisionReporter> colliders;
        
        // Initial state references
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Vector3 _initialScale;

        private bool _isOnline;

        private void Start()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _initialScale = transform.localScale;
            
            _checkBoxPlaces = ReadCheckBoxPlaces();
            SetColliderEvents();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _isOnline = false;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _isOnline = false;
        }

        // COLLISION METHODS
        
        private void SetColliderEvents()
        {
            for (var i = 0; i < colliders.Count; i++)
            {
                var i1 = i;
                colliders[i].OnTriggerEntered += (other) =>
                {
                    if (!other.CompareTag("PenTip") || (_isOnline && !IsOwner)) return;
                    ToggleCheckBoxSynced(i1);
                };
            }
        }

        private void ToggleCheckBoxSynced(int index)
        {
            if (index > _checkBoxStates.Length - 1) return;
            
            if (!_isOnline)
            {
                _checkBoxStates[index] = !_checkBoxStates[index];  
                UpdateCheckBox(index); 
                audioSource.Play();
            }
            else
            {
                ToggleCheckBoxStateRPC(index);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void ToggleCheckBoxStateRPC(int index)
        {
            _checkBoxStates[index] = !_checkBoxStates[index];   
            UpdateCheckBox(index); 
            audioSource.Play();
        }

        // ARRAY METHODS

        private int[] ReadCheckBoxPlaces()
        {
            int[] positions = new int[checkBoxNum];
            var text = mainTextUGUI.text;
            int pointer = 0;
            
            for (int i = 0; i < positions.Length; i++)
            {
                while (true)
                {
                    // End of the text
                    if (pointer >= text.Length) return positions;
                    
                    // Find Check Box
                    if (text[pointer].Equals(CBox))
                    {
                        positions[i] = pointer;
                        pointer++;
                        break;
                    }
                    pointer++;
                }
            }

            return positions;
        }
        
        // Single position, toggles value
        private void UpdateCheckBox(int index)
        {
            var text = mainTextUGUI.text.ToCharArray();
            var pos = _checkBoxPlaces[index];
            
            if (_checkBoxStates[index])
            {
                text[pos] = CCheck;
            }
            else
            {   
                text[pos] = CBox;
            }

            mainTextUGUI.text = new string(text);
        }
        // Whole array, sets to a single value
        private void SetCheckBoxState(int[] index, bool state)
        {
            var text = mainTextUGUI.text.ToCharArray();

            foreach (int pos in index)
            {
                text[pos] = state ? CCheck : CBox;
            }

            mainTextUGUI.text = new string(text);
        }
        
        // INTERACTION METHODS

        public void Grab()
        {
            RequestOwnershipServerRPC();
        }
        [ServerRpc(RequireOwnership = false)]
        private void RequestOwnershipServerRPC(ServerRpcParams rpcParams = default)
        {
            // Change ownership
            ulong grabbingClientId = rpcParams.Receive.SenderClientId;
            GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);
        }
        
        //////////////////      RESTORATION     //////////////////
        
        public void RestoreToStartConditionsSynced()
        {
            if (!_isOnline)
            {
                RestoreToStartConditions();
            }
            else
            {
                RestoreToStartConditionsRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void RestoreToStartConditionsRPC()
        {
            RestoreToStartConditions();
        }
        private void RestoreToStartConditions()
        {
            // Set transform to initial values
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            transform.localScale = _initialScale;
            
            // Stop in physics
            var rBody = GetComponent<Rigidbody>();
            if (rBody is not null)
            {
                rBody.linearVelocity = Vector3.zero;
                rBody.angularVelocity = Vector3.zero;
            }
            
            // Set all checkboxes to false
            SetCheckBoxState(_checkBoxPlaces, false);
            
            // Set all checkbox states to false
            for (int i = 0; i <_checkBoxStates.Length; i++)
            {
                _checkBoxStates[i] = false;
            }
        }
    }
}
