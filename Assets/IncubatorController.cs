using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class IncubatorController : MonoBehaviour
{
    [Header("Door Elements")]
    [SerializeField] private Transform handler;
    [SerializeField] private Transform grabbableHandler;
    [SerializeField] private HingeJoint joint;
    
    [SerializeField] private float maxDistance;
    private XRGrabInteractable _interactable;
    
    [Header("Sockets")]
    [SerializeField] private List<XRSocketInteractor>  sockets;
    [SerializeField] private TextMeshProUGUI visualText;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _interactable = grabbableHandler.GetComponent<XRGrabInteractable>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(handler.position, grabbableHandler.position) > maxDistance)
        {
            ForceRelease();
        }
    }
    
    private void ForceRelease()
    {
        // Loop through all interactors currently holding this object
        foreach (var interactor in _interactable.interactorsSelecting)
        {
            _interactable.interactionManager.SelectExit(interactor, _interactable);
        }
    }

    public void GrabStart()
    {
        joint.useSpring = false;
    }
    public void GrabEnd()
    {
        joint.useSpring = true;
        
        grabbableHandler.transform.position = handler.position;
        grabbableHandler.transform.rotation = handler.rotation;

        Rigidbody rbHandler = handler.GetComponent<Rigidbody>();
        rbHandler.linearVelocity = Vector3.zero;
        rbHandler.angularVelocity = Vector3.zero;
    }
    
    
    public void DebugHelloWorld(string origin)
    {
        Debug.Log($"[INCUBATOR]: Hello, I'm Mr. Incubator, this is {origin}'s show, I eat the Petri.");
    }
}
