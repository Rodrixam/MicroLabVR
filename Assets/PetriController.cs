using System;
using System.Collections.Generic;
using Scripts;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PetriController : NetworkBehaviour
{
    // ACTIVITIES:
    // - DRAW LINES
    // - SEED MICROBES
    private readonly NetworkVariable<int> _activityIndex = 
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<bool> _lpDetachable = 
        new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<bool> _lpIsAttached = 
        new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private bool _isOnline;
    
    // References
    
    [SerializeField] private PetriLineTask lineTask;
    [SerializeField] private LittlePetriController littlePetri;
    
    [SerializeField] private List<Collider> colliders;
    [SerializeField] private XRSocketInteractor socket;

    // Network Variables

    private readonly NetworkVariable<bool> _netIsGrabbed =
        new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    
    private void Awake()
    {
        // Subscribe to socket events
        socket.selectEntered.AddListener(OnObjectPlaced);
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
        // Always unsubscribe to avoid leaks
        socket.selectEntered.RemoveListener(OnObjectPlaced);
    }

    private void OnEnable()
    {   
        littlePetri.SetCollidersState(false);
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

        // ------------------------- LOCAL -------------------------
        if (!_isOnline)
        {
            // Set grabbed true
            _netIsGrabbed.Value = true;
        }
        // ------------------------- ONLINE -------------------------
        else
        {
            // If online, it must request ownership first
            RequestGrabServerRPC(true);
        }

        if (_lpDetachable.Value && _lpIsAttached.Value)
        {
            SetCollidersState(false);
            littlePetri.SetCollidersState(true);
        }
    }

    public void Drop()
    {
        // ------------------------- LOCAL -------------------------
        if (!_isOnline || (_isOnline && IsOwner))
        {
            // Set grabbed false
            _netIsGrabbed.Value = false;
        }
        else
        {
            Debug.LogError("Petri dropped by not owner... a fix for delay in ownership assignation might be needed");
        }
        
        if (_lpDetachable.Value)
        {
            SetCollidersState(true);
            if (_lpIsAttached.Value)
            {
                littlePetri.SetCollidersState(false);   
            }
        }
    }
    
    private void OnObjectPlaced(SelectEnterEventArgs args)
    {
        littlePetri = args.interactableObject.transform.GetComponentInChildren<LittlePetriController>();
    }
    
    // RPC METHODS

    [ServerRpc(RequireOwnership = false)]
    private void RequestGrabServerRPC(bool value, ServerRpcParams rpcParams = default)
    {
        // Change ownership
        ulong grabbingClientId = rpcParams.Receive.SenderClientId;
        GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);

        // Change variables
        SetGrabbedClientRpc(value);
    }

    [Rpc(SendTo.Owner)]
    private void SetGrabbedClientRpc(bool value)
    {
        // Change grab state
        _netIsGrabbed.Value = value;
    }
    
    // ACCESS METHODS
    
    public bool IsGrabbed()
    {
        return _netIsGrabbed.Value;
    }

    public void CompleteActivity()
    {
        if (_isOnline && !IsOwner) return;
        
        _activityIndex.Value++;

        switch (_activityIndex.Value)
        {
            case 1:
                lineTask.enabled = false;
                _lpDetachable.Value = true;
                
                if (_netIsGrabbed.Value)
                {
                    if (!_isOnline)
                    {
                        littlePetri.SetCollidersState(true);
                        SetCollidersState(false);
                    }
                    else
                    {
                        SetLittlePetriCollidersStateRPC(true);
                        SetCollidersStateRPC(false);
                    }
                }
                break;
        }
    }

    public void DetachLittlePetri()
    {
        if (!_isOnline || (_isOnline && IsOwner))
        {
            _lpIsAttached.Value = false;   
        }
    }

    public void AttachLittlePetri()
    {
        if (!_isOnline || (_isOnline && IsOwner))
        {
            _lpIsAttached.Value = true;   
        }

        if (!_netIsGrabbed.Value)
        {
            littlePetri.SetCollidersState(false);
        }
    }
    
    private void SetCollidersState(bool state)
    {
        foreach(var col in colliders)
        {
            col.enabled = state;
        }
    }


    [Rpc(SendTo.ClientsAndHost)]
    private void SetCollidersStateRPC(bool state)
    {
        foreach(var col in colliders)
        {
            col.enabled = state;
        }
    }
    
    
    [Rpc(SendTo.ClientsAndHost)]
    private void SetLittlePetriCollidersStateRPC(bool state)
    {
        littlePetri.SetCollidersState(state);
    }
}
