using System.Collections.Generic;
using Scripts;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class LidPetriController : NetworkBehaviour
{
    [SerializeField] private List<Collider> colliders;
    [SerializeField] private XRSocketInteractor socket;
    [SerializeField] private PetriController  petriController;
    public PetriController PetriController => petriController;
    
    private readonly NetworkVariable<bool> _lpDetachable = 
        new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<bool> _lpIsAttached = 
        new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<bool> _isGrabbed =
        new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public bool isGrabbed => _isGrabbed.Value;

    private bool _isOnline;
    
    private void OnEnable()
    {   
        petriController.SetCollidersState(false);
    }
    
    // SOCKET EVENTS
    
    private void Start()
    {
        Debug.Log(socket);
        socket.selectEntered.AddListener(OnObjectPlaced);
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
        socket.selectEntered.RemoveListener(OnObjectPlaced);
    }
    
    // NETWORK METHODS
        
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
    
    // INTERACTION METHODS

    public void Grab()
    {
        Debug.Log("[LID PETRI]: I'm grabbing");
        
        // ------------------------- LOCAL -------------------------
        if (!_isOnline)
        {
            _isGrabbed.Value = true;
            
            if (_lpDetachable.Value && _lpIsAttached.Value)
            {
                // Change colliders
                SetCollidersState(false);
                petriController.SetCollidersState(true);
            
                // Call grab from main
                petriController.Grab();
            }
        }
        // ------------------------- ONLINE -------------------------
        else
        {
            // If online, it must request ownership first
            RequestGrabServerRPC();
        }
    }
    
    public void Drop()
    {
        if (!_lpDetachable.Value) return;
        
        // Change colliders
        SetCollidersState(true);
        if (_lpIsAttached.Value)
        {
            petriController.SetCollidersState(false);   
        }
            
        // Call drop from main
        petriController.Drop();
        
        // Call grab from the line task if not completed
        if (!petriController.LineTask.allLinesDrawn)
        {
            petriController.LineTask.Drop();
        }
    }
    
    // SOCKET METHODS
    
    private void OnObjectPlaced(SelectEnterEventArgs args)
    {
        petriController = args.interactableObject.transform.GetComponentInChildren<PetriController>();
    }
    
    public void AttachLittlePetri()
    {
        if (!_isOnline || (_isOnline && IsOwner))
        {
            _lpIsAttached.Value = true;   
        }

        petriController.SetLidReference(this);
        if(isGrabbed)
        {
            SetCollidersState(false);
        }
    }
    
    public void DetachLittlePetri()
    {
        if (!_isOnline || (_isOnline && IsOwner))
        {
            _lpIsAttached.Value = false;   
        }
    }
    
    // RPC METHODS

    [ServerRpc(RequireOwnership = false)]
    private void RequestGrabServerRPC(ServerRpcParams rpcParams = default)
    {
        // Change ownership
        ulong grabbingClientId = rpcParams.Receive.SenderClientId;
        GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);

        // Change variables
        GrabClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void GrabClientRpc()
    {
        if (IsOwner)
        {
            _isGrabbed.Value = true;
        }
        
        if (_lpDetachable.Value && _lpIsAttached.Value)
        {
            // Change colliders
            SetCollidersState(false);
            petriController.SetCollidersState(true);
            
            // Call grab from main
            petriController.Grab();
        }
    }
    
    // OTHERS
    
    public void SetCollidersState(bool state)
    {
        foreach(var col in colliders)
        {
            col.enabled = state;
        }
    }

    public void SetDetachable(bool state)
    {
        _lpDetachable.Value = state;
    }
}
