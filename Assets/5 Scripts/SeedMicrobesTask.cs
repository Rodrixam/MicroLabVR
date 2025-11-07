using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class SeedMicrobesTask : NetworkBehaviour
    {
        [Header("References")] [SerializeField]
        private XRGrabInteractable grabInteractable;

        [SerializeField] private PetriController mainController;

        [Header("Collision Reporters")] [SerializeField]
        private CollisionReporter eColiReporter;

        [SerializeField] private CollisionReporter sAureusReporter;
        [SerializeField] private CollisionReporter sporesReporter;

        [Header("Seed Lines")] [SerializeField]
        private Image eColiLines;

        [SerializeField] private Image sAureusLines;
        [SerializeField] private Image sporesLines;

        [Header("Renderers")] [SerializeField] private MeshRenderer eColiColliderRenderer;
        [SerializeField] private MeshRenderer sAureusColliderRenderer;
        [SerializeField] private MeshRenderer sporesColliderRenderer;

        private bool _eColiSeeded, _sAureusSeeded, _sporesSeeded;
        private int _eColiInterval, _sAureusInterval, _sporesInterval;
        public int eColiInterval => _eColiInterval;
        public int sAureusInterval => _sAureusInterval;
        public int sporesInterval => _sporesInterval;

        private bool _isOnline;

        // NETWORK BEHAVIOUR METHODS

        private void Awake()
        {
            // Deactivate sample lines
            eColiLines.gameObject.SetActive(false);
            sAureusLines.gameObject.SetActive(false);
            sporesLines.gameObject.SetActive(false);

            // Deactivate renderer colliders
            eColiColliderRenderer.enabled = false;
            sAureusColliderRenderer.enabled = false;
            sporesColliderRenderer.enabled = false;

            grabInteractable.selectEntered.AddListener(Grab);
        }

        public override void OnDestroy()
        {
            grabInteractable.selectEntered.RemoveListener(Grab);
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

        void Start()
        {   
            // SET COLLISION EVENTS
            
            eColiReporter.OnTriggerEntered += (other) =>
            {
                // skip cases
                if (_eColiSeeded || !other.CompareTag("AsaTip")) return;
            
                // asa values not valid
                var asa = other.GetComponentInParent<AsaController>();
                if (asa is null || asa.sample != TubePickSampleTask.Samples.EColi) return;
            
                // store values
                _eColiSeeded = true;
                _eColiInterval = asa.sampleInterval;
            
                // set visuals
                eColiLines.gameObject.SetActive(true);
                eColiColliderRenderer.enabled = false;
            };
        
            sAureusReporter.OnTriggerEntered += (other) =>
            {
                // skip cases
                if (_sAureusSeeded || !other.CompareTag("AsaTip")) return;
            
                // asa values not valid
                var asa = other.GetComponentInParent<AsaController>();
                if (asa is null || asa.sample != TubePickSampleTask.Samples.SAureus) return;
            
                // store values
                _sAureusSeeded = true;
                _sAureusInterval = asa.sampleInterval;
            
                // set visuals
                sAureusLines.gameObject.SetActive(true);
                sAureusColliderRenderer.enabled = false;
            };
        
            sporesReporter.OnTriggerEntered += (other) =>
            {
                // skip cases
                if (_sporesSeeded || !other.CompareTag("AsaTip")) return;
            
                // asa values not valid
                var asa = other.GetComponentInParent<AsaController>();
                if (asa is null || asa.sample != TubePickSampleTask.Samples.Spores) return;
            
                // store values
                _sporesSeeded = true;
                _sporesInterval = asa.sampleInterval;
            
                // set visuals
                sporesLines.gameObject.SetActive(true);
                sporesColliderRenderer.enabled = false;
            };
        }

        // INTERACTION METHODS
        
        private void Grab(SelectEnterEventArgs args)
        {
            if (mainController.activityIndex <= 0 || !IsPickedByHand(args)) return;
            
            if(!_isOnline)
            {
                EnableColliderRenderer(true);
            }
            else
            {
                EnableColliderRendererRPC(true);
            }
        }
        
        public void Drop()
        {
            if (mainController.activityIndex <= 0) return;
        
            if(!_isOnline)
            {
                EnableColliderRenderer(false);
            }
            else
            {
                EnableColliderRendererRPC(false);
            }
        }
        
        private void EnableColliderRenderer(bool state)
        {
            if (state)
            {
                if (!_eColiSeeded)
                {
                    eColiColliderRenderer.enabled = true;
                }
                if (!_sAureusSeeded)
                {
                    sAureusColliderRenderer.enabled = true;
                }
                if (!_sporesSeeded)
                {
                    sporesColliderRenderer.enabled = true;
                }
            }
            else
            {
                eColiColliderRenderer.enabled = false;
                sAureusColliderRenderer.enabled = false;
                sporesColliderRenderer.enabled = false;
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void EnableColliderRendererRPC(bool state)
        {
            EnableColliderRenderer(state);
        }
    
        private bool IsPickedByHand(SelectEnterEventArgs args)
        {
            if (args.interactorObject.transform.CompareTag("LeftHand") ||
                args.interactorObject.transform.CompareTag("RightHand"))
            {
                return true;
            }
            return false;
        }
        
        // ACCESS METHODS
    
        public bool IsCompleted()
        {
            return _eColiSeeded && _sAureusSeeded && _sporesSeeded;
        }

        // It's called from IncubatorController, from which it's already synchronized
        public void CallSetMicrobesImages(TubePickSampleTask.Samples microbe, Sprite newImage, Color newColor)
        {
            
            SetMicrobesImages(microbe, newImage, newColor);
            /*
            if (!_isOnline)
            {
                SetMicrobesImages(microbe, newImage, newColor);
            }
            else
            {
                SetMicrobesImagesRPC(microbe, newImage, newColor);
            }*/
        }
        
        private void SetMicrobesImages(TubePickSampleTask.Samples microbe, Sprite newImage, Color newColor)
        {   
            switch (microbe)
            {
                case TubePickSampleTask.Samples.EColi:
                    eColiLines.sprite = newImage;
                    eColiLines.color = newColor;
                    break;
                case TubePickSampleTask.Samples.SAureus:
                    sAureusLines.sprite = newImage;
                    sAureusLines.color = newColor;
                    break;
                case TubePickSampleTask.Samples.Spores:
                    sporesLines.sprite = newImage;
                    sporesLines.color = newColor;
                    break;
            }
        }

        /*
        [Rpc(SendTo.ClientsAndHost)]
        private void SetMicrobesImagesRPC(TubePickSampleTask.Samples microbe, Sprite newImage, Color newColor)
        {
            SetMicrobesImages(microbe, newImage, newColor);
        }*/
    }
}
