using UnityEngine;

public class ModuleController : MonoBehaviour
{
    [SerializeField] private GameObject toolsPrefab;
    [SerializeField] private Transform toolsInstantiated;

    public void RestoreTools()
    {
        Destroy(toolsInstantiated.gameObject);
        toolsInstantiated = Instantiate(toolsPrefab, transform).transform;
    }
}
