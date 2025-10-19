using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
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


    void Start()
    {
        // Door elements
        _interactable = grabbableHandler.GetComponent<XRGrabInteractable>();
        
        // Sockets
        foreach (var socket in sockets)
        {
            socket.selectEntered.AddListener(ValidatePetri);
            socket.selectExited.AddListener(RemovePetri);
        }
        
        // Audio
        _audioSource = GetComponent<AudioSource>();
    }
    
    void Update()
    {
        if (Vector3.Distance(handler.position, grabbableHandler.position) > maxDistance)
        {
            ForceRelease();
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
        
        grabbableHandler.transform.position = handler.position;
        grabbableHandler.transform.rotation = handler.rotation;

        Rigidbody rbHandler = handler.GetComponent<Rigidbody>();
        rbHandler.linearVelocity = Vector3.zero;
        rbHandler.angularVelocity = Vector3.zero;
    }
    
    private void ForceRelease()
    {
        // Loop through all interactors currently holding this object
        foreach (var interactor in _interactable.interactorsSelecting)
        {
            _interactable.interactionManager.SelectExit(interactor, _interactable);
        }
    }
    
    // SOCKET METHODS

    private void ValidatePetri(SelectEnterEventArgs args)
    {
        var lid = args.interactableObject.transform.GetComponent<LidPetriController>();
        if (lid is null || !lid.PetriController.SeedTask.IsCompleted()) return;
        
        _validPetri.Add(lid);
        visualText.text = $"{_validPetri.Count}/4";
            
        if (_validPetri.Count >= 4) { _completed = true; }
    }

    private void RemovePetri(SelectExitEventArgs args)
    {
        var lid = args.interactableObject.transform.GetComponent<LidPetriController>();
        if (lid is null) return;
        
        _validPetri.Remove(lid);
        visualText.text = $"{_validPetri.Count}/4";
        
        if (_validPetri.Count < 4) { _completed = false; }
    }

    public void CookThem()
    {
        if (!_completed)
        {
            _audioSource.clip = wrongBuzz;
            _audioSource.Play();
            return;
        }

        foreach (var lid in _validPetri)
        {
            lid.PetriController.RemoveAgar();
            
            int eColiInterval = lid.PetriController.SeedTask.eColiInterval;
            lid.PetriController.SeedTask.SetMicrobesImages(TubePickSampleTask.Samples.EColi, eColiSprites[eColiInterval]);
            
            int sAureusInterval = lid.PetriController.SeedTask.sAureusInterval;
            lid.PetriController.SeedTask.SetMicrobesImages(TubePickSampleTask.Samples.SAureus, eColiSprites[sAureusInterval]);
            
            int sporesInterval = lid.PetriController.SeedTask.eColiInterval;
            lid.PetriController.SeedTask.SetMicrobesImages(TubePickSampleTask.Samples.Spores, eColiSprites[sporesInterval]);
        }
        
        _audioSource.clip = correctBell;
        _audioSource.Play();
    }
    
    // OTHERS
    
    public void DebugHelloWorld(string origin)
    {
        Debug.Log($"[INCUBATOR]: Hello, I'm Mr. Incubator, this is {origin}'s show, I eat the Petri.");
    }
}
