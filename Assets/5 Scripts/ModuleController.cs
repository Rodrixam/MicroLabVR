using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable Unity.PerformanceCriticalCodeInvocation
// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class ModuleController : NetworkBehaviour
    {
        [Header("Reset Button")] 
        [SerializeField] private Image progressCircle;
        [SerializeField] private float pushTime;
        
        private readonly NetworkVariable<bool> _isPushed = 
            new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<float> _pushCounter = 
            new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        [Header("Clipboard and Pen")]
        [SerializeField] private ClipboardController clipboard;
        [SerializeField] private GrabOwnership pen;

        [Header("Petri Plates and Incubator")]
        [SerializeField] private List<LidPetriController> lids;
        [SerializeField] private List<PetriController> petris;
        [SerializeField] private IncubatorController incubator;

        [Header("Burner and Boiler")]
        [SerializeField] private BunsenController burner;
        [SerializeField] private LighterController lighter;
        [SerializeField] private TripodController tripod;
        [SerializeField] private BeakGlassController vaso;

        [Header("Tubes and Asa")]
        [SerializeField] private List<TubeController> tubes;
        [SerializeField] private AsaController asa;
        [SerializeField] private TubeRackController tubeRack;

        private bool _isOnline;
        
        // NETWORK BEHAVIOUR METHODS

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // Tools reset to default
            _isOnline = true;
            RestoreToolsSynced();
            
            // Button stuff
            _pushCounter.OnValueChanged += UpdateButtonUI;
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            // Tools reset to default
            _isOnline = false;
            RestoreToolsSynced();
            
            // Button stuff
            _pushCounter.OnValueChanged -= UpdateButtonUI;
        }
        
        private void Start()
        {
            // Configuration for progress circle image to fill
            progressCircle.type = Image.Type.Filled;
            progressCircle.fillClockwise = true;
            progressCircle.fillMethod = Image.FillMethod.Radial360;
            progressCircle.fillOrigin = (int) Image.Origin360.Top;
            progressCircle.fillAmount = 0;
            
            UpdateButtonUI(0, _pushCounter.Value);
        }

        private void Update()
        {
            if (_isOnline && !IsOwner) return;

            // If is pushed
            if (_isPushed.Value && _pushCounter.Value < pushTime)
            {
                // Timer
                _pushCounter.Value += Time.deltaTime;
                if (_pushCounter.Value >= pushTime)
                {
                    RestoreToolsSynced();
                    _isPushed.Value = false;
                    _pushCounter.Value = 0;
                }

                // Update UI
                if (!_isOnline)
                {
                    UpdateButtonUI(0, _pushCounter.Value);
                }
            }
        }


        // OTHERS
        
        // TODO REWORK ONLINE CONSOLE: 
        //          LA CONSOLA ONLINE DEBE DETECTAR SI ESTÁ CONECTADO A INTERNET PARA FUNCIONAR
        //          SE DEBE PODER CREAR ROOMS DISTINTAS, CON UN LÍMITE PARA 2 JUGADORES, Y PODER ELEGIR UNIRSE A UNO

        ///////////     BUTTON SYSTEM     ///////////
        public void SetButtonState(bool state)
        {
            if (!_isOnline)
            {
                _isPushed.Value = state;
                
                if (state) return;
                _pushCounter.Value = 0;
                UpdateButtonUI(0,0);
            }
            else
            {
                SetIsPushedRPC(state);
            }
        }
        [Rpc(SendTo.Owner)]
        private void SetIsPushedRPC(bool state)
        {
            _isPushed.Value = state;
            if (!state)
            {
                _pushCounter.Value = 0;
            }
        }

        private void UpdateButtonUI(float value, float newValue)
        {
            progressCircle.fillAmount = Mathf.Clamp01(newValue / pushTime);
        }
        
        ///////////     RESTORATION     ///////////
        
        private void RestoreToolsSynced()
        {
            // Restore Tools
            if (!_isOnline)
            {
                CallAllToolsToRestore();
            }
            else
            {
                RestoreToolsRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void RestoreToolsRPC()
        {
            if (!IsHost) return;
            CallAllToolsToRestore();
        }

        private void CallAllToolsToRestore()
        {
            // Clipboard
            clipboard.RestoreToStartConditionsSynced();
            // Pen
            pen.RestoreToStartConditions();
            
            
            // Petri Lids
            foreach (var lid in lids) 
                lid.RestoreToStartConditions();
            // Little Petris
            foreach (var petri in petris)
                petri.RestoreToStartConditions();
            // Incubator
            incubator.RestoreToStartConditions();
            
            
            // Bunsen Burner
            burner.RestoreToStartConditionsSynced();
            // Lighter
            lighter.RestoreToStartConditions();
            // Tripod
            tripod.RestoreToStartConditions();
            // Vaso
            vaso.RestoreToStartConditions();
            
            
            // Tubes
            foreach (var tube in tubes)
                tube.RestoreToStartConditions();
            // Asa
            asa.RestoreToStartConditions();
            // TubeRack
            tubeRack.RestoreToStartConditions();
        }
    }
}
