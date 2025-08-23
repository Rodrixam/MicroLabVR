using UnityEngine;

namespace ModularLab.Scripts
{
    public class NewMonoBehaviourScript : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            var labGen = FindAnyObjectByType<LabGenerator>();
            GenerateModules(labGen.GetModuleNum());
        }

        void GenerateModules(int num)
        {
            Debug.Log("Generating " + num + " modules");
        }
    }
}
