using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class PetriController : MonoBehaviour
    {
        [Header("Pen")] 
    
        private bool _isGrabbed = false;
        private bool _finishingLine = false;
        private bool _allLinesDrawn = false;
    
        private int _linesProgress = 0;
    
        [SerializeField] private GameObject divisionLines;
        [SerializeField] private List<GameObject> interactivePoints;
        [SerializeField] private List<LineRenderer> lineRenderers;
        [SerializeField] private ParticleSystem particles;

        private Transform _penTip = null;
    
        void Awake()
        {
            foreach (var reporter in GetComponentsInChildren<CollisionReporter>())
            {
                reporter.onTriggerEntered += (other) =>
                {
                    if (!reporter.CompareTag("InteractivePoint")) return;
                    PointReached(other);
                };
            
                reporter.onTriggerExited += (other) =>
                {
                    if (!reporter.CompareTag("LimitZone")) return;
                    PenLifted(other);
                };
            }
        }
        void Start()
        {
            // Deactivate completed image
            divisionLines.SetActive(_allLinesDrawn);

            // Deactivate interactive points
            foreach (var p in interactivePoints)
            {
                p.SetActive(false);
            }
        
            // Deactivate line renderers
            foreach (var lr in lineRenderers)
            {
                lr.positionCount = 0;
            }
        }

        private void Update()
        {
            // Update line renderers for complete lines
            if (!_allLinesDrawn)
            {
                for (int i = 0; i <= _linesProgress; i++)
                {
                    if (lineRenderers[i].positionCount > 1)
                    {
                        lineRenderers[i].SetPosition(0, interactivePoints[i + 1].transform.position);
                        lineRenderers[i].SetPosition(1, interactivePoints[0].transform.position);
                    }
                }
            }
        
            // Update line renderer following pentip
            if (!_allLinesDrawn && _isGrabbed && _finishingLine && _penTip != null && lineRenderers[_linesProgress].positionCount > 1)
            {
                lineRenderers[_linesProgress].SetPosition(0, interactivePoints[_linesProgress + 1].transform.position);
                lineRenderers[_linesProgress].SetPosition(1, _penTip.position);
            }
        }

        public void Grab()
        {
            Debug.Log("Petri grabbed");
            if (_allLinesDrawn) return;
        
            // Set grabbed true
            _isGrabbed = true;
        
            // Activate interactive point
            interactivePoints[_linesProgress + 1].SetActive(true);
        }
    
        public void Drop()
        {
            Debug.Log("Petri dropped");
            if (_allLinesDrawn) return;
        
            // Set grabbed true
            _isGrabbed = false;
        
            // Deactivate interactive points
            foreach (var p in interactivePoints)
            {
                p.SetActive(false);
            }
        
            // Deactivate uncompleted line renderers
            for(int i = _linesProgress; i < lineRenderers.Count; i++)
            {
                lineRenderers[i].positionCount = 0;
            }
        }

        private void PointReached(Collider other)
        {
            if (!_isGrabbed || !other.CompareTag("PenTip") || _allLinesDrawn) return;
        
            // Set penTip transform if null
            if (_penTip == null)
            {
                _penTip = other.transform;
            }

            // Beginning line draw
            if (!_finishingLine)
            {
                _finishingLine = true;
            
                // Activate center point
                interactivePoints[_linesProgress + 1].SetActive(false);
                interactivePoints[0].SetActive(true);
            
                // Activate line renderer
                lineRenderers[_linesProgress].positionCount = 2;
                lineRenderers[_linesProgress].SetPosition(0, interactivePoints[_linesProgress + 1].transform.position);
                lineRenderers[_linesProgress].SetPosition(1, interactivePoints[_linesProgress + 1].transform.position);
            }
            // Finishing line draw
            else
            {
                _linesProgress++;
                _finishingLine = false;
            
                // Complete drawing
                if (_linesProgress > 2)
                {
                    _allLinesDrawn = true;
                    divisionLines.SetActive(true);
                
                    // Deactivate interactive points
                    foreach (var p in interactivePoints)
                    {
                        p.SetActive(false);
                    }
                
                    // Deactivate line renderers
                    for(int i = 0; i < lineRenderers.Count; i++)
                    {
                        lineRenderers[i].positionCount = 0;
                    }
                
                    // Victory particles
                    particles.Play();
                    return;
                }
            
                // Activate next point
                interactivePoints[0].SetActive(false);
                interactivePoints[_linesProgress + 1].SetActive(true);
            }
        }

        private void PenLifted(Collider other)
        {
            if (_allLinesDrawn) return;
        
            _finishingLine = false;
            _penTip = null;
        
            // Deactivate center point (just in case)
            interactivePoints[0].SetActive(false);
            interactivePoints[_linesProgress + 1].SetActive(true);
        
            // Deactivate line renderer
            lineRenderers[_linesProgress].positionCount = 0;
        }
    }
}
