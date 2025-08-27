using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ModularLab.Scripts
{
    public class NewMonoBehaviourScript : MonoBehaviour
    {
        [SerializeField] private List<GameObject> backWalls;

        [SerializeField] private List<GameObject> modules;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            var labGen = FindAnyObjectByType<LabGenerator>();
            GenerateModules(labGen.GetModuleNum());
        }

        void GenerateModules(int num)
        {
            Debug.Log("Generating " + num + " modules");

            if (num > 3)
            {
                backWalls[0].SetActive(false);
            }
            if (num > 6)
            {
                backWalls[1].SetActive(false);
            }

            for (int i = 0; i < num; i++)
            {
                modules[i].SetActive(true);
            }
        }
    }
}
