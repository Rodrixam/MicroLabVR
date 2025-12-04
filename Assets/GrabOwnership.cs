using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class GrabOwnership : NetworkBehaviour
{
    private XRGrabInteractable _grabInteractable;
    private bool _isOnline;
    
    // Initial state references
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Vector3 _initialScale;

    private void Start()
    {
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
        _initialScale = transform.localScale;
        
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _grabInteractable.selectEntered.AddListener(Grab);
    }

    public override void OnDestroy()
    {
        _grabInteractable.selectEntered.RemoveListener(Grab);
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        _isOnline = true;
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        _isOnline = false;
        base.OnNetworkDespawn();
    }

    private void Grab(SelectEnterEventArgs args)
    {
        if (!_isOnline) return;
        
        RequestOwnershipServerRPC();
    }
    [ServerRpc(RequireOwnership = false)]
    private void RequestOwnershipServerRPC(ServerRpcParams rpcParams = default)
    {
        // Change ownership
        ulong grabbingClientId = rpcParams.Receive.SenderClientId;
        GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);
    }
    
    public void RestoreToStartConditions()
    {
        // Transform
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;
        transform.localScale = _initialScale;
    }
}
