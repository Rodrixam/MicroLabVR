using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class IncubatorController : NetworkBehaviour
    {
        private bool _isOnline;
        
        [Header("Door Elements")]
        [SerializeField] private Transform handler;
        [SerializeField] private Transform grabbableHandler;
        [SerializeField] private HingeJoint joint;
    
        [SerializeField] private float maxDistance;
        private XRGrabInteractable _interactable;
    
        [Header("Sockets")]
        [SerializeField] private List<XRSocketInteractor>  sockets;
        [SerializeField] private TextMeshProUGUI visualText;

        private readonly List<LidPetriController> _validPetri = new ();
        private bool _completed; 

        [Header("Cultivation Sprites")] 
        [SerializeField] private List<Sprite> eColiSprites;
        [SerializeField] private List<Sprite> sAureusSprites;
        [SerializeField] private List<Sprite> sporesSprites;


        [Header("Sound Effects")] 
        [SerializeField] private AudioClip correctBell;
        [SerializeField] private AudioClip wrongBuzz;
        private AudioSource _audioSource;

        
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

        void Start()
        {
            // Door elements
            _interactable = grabbableHandler.GetComponent<XRGrabInteractable>();
        
            // Sockets
            foreach (var socket in sockets)
            {
                socket.selectEntered.AddListener(ValidatePetriSynced);
                socket.selectExited.AddListener(RemovePetriSynced);
            }
        
            // Audio
            _audioSource = GetComponent<AudioSource>();
        }
    
        void Update()
        {
            if (_isOnline && !IsOwner) return;
            
            if (Vector3.Distance(handler.position, grabbableHandler.position) > maxDistance)
            {
                ForceReleaseSynced();
            }
        }
    
        // HANDLER METHODS
    
        public void GrabStart()
        {
            joint.useSpring = false;
        }
        public void GrabEnd()
        {
            joint.useSpring = true;
        
            grabbableHandler.transform.localPosition = Vector3.zero;
            grabbableHandler.transform.localRotation = Quaternion.identity;

            Rigidbody rbHandler = handler.GetComponent<Rigidbody>();
            rbHandler.linearVelocity = Vector3.zero;
            rbHandler.angularVelocity = Vector3.zero;
        }
    
        private void ForceReleaseSynced()
        {
            if(!_isOnline)
            {
                // Loop through all interactors currently holding this object
                var interactors = new List<IXRSelectInteractor>(_interactable.interactorsSelecting.ToArray());
                foreach (var interactor in interactors)
                {
                    _interactable.interactionManager.SelectExit(interactor, _interactable);
                }   
            }
            else
            {
                ForceReleaseRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void ForceReleaseRPC()
        {
            // Loop through all interactors currently holding this object
            var interactors = new List<IXRSelectInteractor>(_interactable.interactorsSelecting.ToArray());
            foreach (var interactor in interactors)
            {
                _interactable.interactionManager.SelectExit(interactor, _interactable);
            }   
        }
        
        // SOCKET METHODS
        
        private void ValidatePetriSynced(SelectEnterEventArgs args)
        {
            var lid = args.interactableObject.transform.GetComponent<LidPetriController>();
            if (lid is null || !lid.PetriController.SeedTask.IsCompleted()) return;

            if (!_isOnline)
            {
                _validPetri.Add(lid);
                visualText.text = $"{_validPetri.Count}/4";
            
                if (_validPetri.Count >= 4) { _completed = true; }   
            }
            else
            {
                NetworkObjectReference lidRef = lid.GetComponent<NetworkObject>();
                ValidatePetriRPC(lidRef);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void ValidatePetriRPC(NetworkObjectReference lidReference)
        {
            if (lidReference.TryGet(out NetworkObject networkObject))
            {
                var lid = networkObject.GetComponent<LidPetriController>();
                _validPetri.Add(lid);
                visualText.text = $"{_validPetri.Count}/4";
            
                if (_validPetri.Count >= 4) { _completed = true; }   
            }
        }

        private void RemovePetriSynced(SelectExitEventArgs args)
        {
            var lid = args.interactableObject.transform.GetComponent<LidPetriController>();
            if (lid is null) return;

            if (!_isOnline)
            {
                _validPetri.Remove(lid);
                visualText.text = $"{_validPetri.Count}/4";
        
                if (_validPetri.Count < 4) { _completed = false; }   
            }
            else
            {
                NetworkObjectReference lidRef = lid.GetComponent<NetworkObject>();
                RemovePetriRPC(lidRef);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void RemovePetriRPC(NetworkObjectReference lidReference)
        {
            if (lidReference.TryGet(out NetworkObject networkObject))
            {
                var lid = networkObject.GetComponent<LidPetriController>();
                _validPetri.Remove(lid);
                visualText.text = $"{_validPetri.Count}/4";
        
                if (_validPetri.Count < 4) { _completed = false; }   
            }
        }

        // INTERACTION METHODS
        
        public void CallCookThem()
        {
            if (!_isOnline)
            {
                CookThem();
            }
            else
            {
                CookThemRPC();
            }
        }
        
        private void CookThem()
        {
            if (!_completed)
            {
                _audioSource.clip = wrongBuzz;
                _audioSource.Play();
                return;
            }

            Color cAux;
            foreach (var lid in _validPetri)
            {
                lid.PetriController.RemoveAgar();
                
                int eColiInterval = lid.PetriController.SeedTask.eColiInterval;
                cAux = Color.white;
                if(eColiSprites[eColiInterval] is null) cAux = Color.clear;
                lid.PetriController.SeedTask.CallSetMicrobesImages(TubePickSampleTask.Samples.EColi, eColiSprites[eColiInterval], cAux);
            
                int sAureusInterval = lid.PetriController.SeedTask.sAureusInterval;
                cAux = Color.white;
                if(sAureusSprites[sAureusInterval] is null) cAux = Color.clear;
                lid.PetriController.SeedTask.CallSetMicrobesImages(TubePickSampleTask.Samples.SAureus, sAureusSprites[sAureusInterval], cAux);
            
                int sporesInterval = lid.PetriController.SeedTask.sporesInterval;
                cAux = Color.white;
                if(sporesSprites[sporesInterval] is null) cAux = Color.clear;
                lid.PetriController.SeedTask.CallSetMicrobesImages(TubePickSampleTask.Samples.Spores, sporesSprites[sporesInterval], cAux);
            }
        
            _audioSource.clip = correctBell;
            _audioSource.Play();   
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void CookThemRPC()
        {
            CookThem();
        }
        
        // ------------------- RESTORATION -------------------

        public void RestoreToStartConditions()
        {
            ForceReleaseSynced();
            
            //Components and References
            if (!_isOnline)
            {
                ResetComponents();
            }
            else
            {
                ResetNetworkComponentsRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void ResetNetworkComponentsRPC()
        {
            ResetComponents();
        }
        private void ResetComponents()
        {
            _validPetri.Clear();
            visualText.text = $"{_validPetri.Count}/4";
            _completed = false;
            
            // detach from sockets
            foreach(var socket in sockets)
            {
                var gi = socket.GetOldestInteractableSelected();
                if (gi != null)
                {
                    socket.interactionManager.CancelInteractableSelection(gi);
                }   
            }
        }
    }
}
