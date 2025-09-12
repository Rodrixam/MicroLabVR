using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace Scripts.Editor
{
    public class CheckMyObjectBeforeBuild : IPreprocessBuildWithReport
    {
        private readonly bool _autoDeactivate = true;
    
        // The order in which this callback runs
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            XRDeviceSimulator[] mustDisable = Object.FindObjectsByType<XRDeviceSimulator>(FindObjectsSortMode.None);
            
            foreach (var go in mustDisable)
            {
                if (go !=null && go.gameObject.activeInHierarchy)
                {
                    if (!_autoDeactivate)
                    {
                        // Stop the build and show an error
                        throw new BuildFailedException(
                            $"Build aborted: '{go.name}' is still active in the scene. " +
                            $"Disable or remove it before building.");
                    }
                    
                    go.gameObject.SetActive(false);
                }
            }
        }
    }
}