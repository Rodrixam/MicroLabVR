using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CottonLidController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private List<Collider> colliders;
    [SerializeField] private GameObject arrowsUI;
    
    [SerializeField] private Transform body;
    [SerializeField] private Transform handler;
    
    private Rigidbody _handlerRb;
    private GameController _gameController;
    private XRGrabInteractable _interactable;
    
    [Header("Parameters")]
    [SerializeField] private float rotationGoal;
    [SerializeField] private float maxDistance;
    
    private Quaternion _prevRotation;
    private float _rotationCounter;
    
    private bool _isGrabbed;
    

    void Start()
    {
        _handlerRb = handler.GetComponent<Rigidbody>();
        _interactable = handler.GetComponent<XRGrabInteractable>();

        _gameController = FindAnyObjectByType<GameController>();
        if (_gameController is null)
        {
            Debug.LogError("[COTTON LID]: GameController not found in scene.");
        }
    }

    void Update()
    {
        if (Vector3.Distance(body.position, handler.position) > maxDistance)
        {
            ForceRelease();
        }

        if (_isGrabbed)
        {
            float angleY = Vector3.SignedAngle(
                _prevRotation * Vector3.forward, // start direction
                body.rotation * Vector3.forward,  // current direction
                Vector3.up                       // rotation axis (Y)
            );
            _rotationCounter += angleY;
            _prevRotation = body.rotation;
            
            Debug.Log($"[COTTON LID]: Rotation {_rotationCounter} of {rotationGoal}.");
            if (Mathf.Abs(_rotationCounter) >= rotationGoal)
            {
                Debug.Log("[COTTON LID]: Rotation completed");
                foreach (var interactor in _interactable.interactorsSelecting)
                {
                    if (interactor.transform.CompareTag("LeftHand"))
                    {
                        if (_gameController.AttachCottonBallToHand(false))
                        {
                            Debug.Log("[COTTON LID]: Attaching cotton ball to left hand.");
                            break;
                        }
                        else
                        {
                            Debug.Log("[COTTON LID]: Cotton ball couldn't attach to left hand.");
                        }
                    }
                    else if (interactor.transform.CompareTag("RightHand"))
                    {
                        if (_gameController.AttachCottonBallToHand(true))
                        {
                            Debug.Log("[COTTON LID]: Attaching cotton ball to right hand.");
                            break;
                        }
                        else
                        {
                            Debug.Log("[COTTON LID]: Cotton ball couldn't attach to right hand.");
                        }
                    }
                }
            }
        }
    }
    
    void LateUpdate()
    {
        if (_isGrabbed)
        {
            Vector3 euler = body.rotation.eulerAngles;
            euler.y = handler.rotation.eulerAngles.y;
            body.rotation = Quaternion.Euler(euler);
        }
    }
    
    private void ForceRelease()
    {
        GrabEnd();
        
        // Loop through all interactors currently holding this object
        foreach (var interactor in _interactable.interactorsSelecting)
        {
            _interactable.interactionManager.SelectExit(interactor, _interactable);
        }
    }

    public void GrabStart()
    {
        _isGrabbed = true;
        
        Vector3 euler = body.rotation.eulerAngles;
        euler.y = handler.rotation.eulerAngles.y;
        body.rotation = Quaternion.Euler(euler);
        
        arrowsUI.SetActive(true);
        
        _prevRotation = handler.rotation;
    }
    public void GrabEnd()
    {
        _rotationCounter = 0;
        _isGrabbed = false;
        
        handler.transform.position = body.position;
        handler.transform.rotation = body.rotation;

        _handlerRb.linearVelocity = Vector3.zero;
        _handlerRb.angularVelocity = Vector3.zero;
        
        arrowsUI.SetActive(false);
    }

    public void SetActiveCollidersState(bool state)
    {
        foreach (var col in colliders)
        {
            col.enabled = state;
        }   
    }
}
