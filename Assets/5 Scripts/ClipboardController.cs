using System.Collections.Generic;
using TMPro;
using UnityEngine;

// ReSharper disable Unity.PerformanceCriticalCodeInvocation
// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class ClipboardController : MonoBehaviour
    {
        // Text references & variables
        [SerializeField] private int checkBoxNum;
        [SerializeField] private TextMeshProUGUI mainTextUGUI;
        
        private const char CBox = '☐';
        private const char CCheck = '√';

        private int[] _checkBoxPlaces;
        
        private readonly bool[] _checkBoxStates = new bool[6];
        
        // Audio references
        [SerializeField] private AudioSource audioSource;

        // Collider references
        [SerializeField] private List<CollisionReporter> colliders;

        private void Start()
        {
            SetCheckBoxPositions();
            SetColliderEvents();
        }
        
        // COLLISION METHODS
        
        private void SetColliderEvents()
        {
            for (var i = 0; i < colliders.Count; i++)
            {
                var i1 = i;
                colliders[i].OnTriggerEntered += (other) =>
                {
                    if (!other.CompareTag("PenTip")) return;

                    _checkBoxStates[i1] = !_checkBoxStates[i1];
                    UpdateCheckBox(i1);
                    audioSource.Play();
                    
                    Debug.Log($"CheckBox {i1} marked as {_checkBoxStates[i1]}");
                };
            }
        }

        private void UpdateCheckBox(int index)
        {
            var text = mainTextUGUI.text.ToCharArray();
            var pos = _checkBoxPlaces[index];
            
            if (_checkBoxStates[index])
            {
                text[pos] = CCheck;
            }
            else
            {   
                text[pos] = CBox;
            }

            mainTextUGUI.text = new string(text);
        }

        // ARRAY METHODS

        private void SetCheckBoxPositions()
        {
            _checkBoxPlaces = ReadCheckBoxPlaces();
            PrintArray(_checkBoxPlaces);
        }

        private int[] ReadCheckBoxPlaces()
        {
            int[] positions = new int[checkBoxNum];
            var text = mainTextUGUI.text;
            int pointer = 0;
            
            for (int i = 0; i < positions.Length; i++)
            {
                while (true)
                {
                    // End of the text
                    if (pointer >= text.Length) return positions;
                    
                    // Find Check Box
                    if (text[pointer].Equals(CBox))
                    {
                        positions[i] = pointer;
                        pointer++;
                        break;
                    }
                    pointer++;
                }
            }

            return positions;
        }

        private void PrintArray(int[] array)
        {
            string str = "";

            for (int i = 0; i < array.Length; i++)
            {
                str += array[i].ToString();
                if(i < array.Length - 1)
                {
                    str += " , ";
                }
            }
            
            Debug.Log(str);
        }
    }
}
