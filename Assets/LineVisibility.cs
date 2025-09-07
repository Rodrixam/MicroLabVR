using System.Collections;
using UnityEngine;

public class LineVisibility : MonoBehaviour
{
    [SerializeField]
    private LineRenderer line;

    public void ShowLine()
    {
        StopAllCoroutines();
        StartCoroutine(ShowingLine(line.startColor, line.endColor));
    }
    
    IEnumerator ShowingLine(Color startColor, Color endColor)
    {
        while (line.startColor.a < 1 || line.endColor.a < 1)
        {
            line.startColor = new Color(startColor.r, startColor.g, startColor.b, line.startColor.a + Time.deltaTime * 4);
            line.endColor = new Color(endColor.r, endColor.g, endColor.b, line.endColor.a + Time.deltaTime * 4);
            Debug.Log("Showing line... " + line.startColor.a + " - " + line.endColor.a);
            yield return null;
        }
    }

    public void HideLine()
    {
        StopAllCoroutines();
        StartCoroutine(HidingLine(line.startColor, line.endColor));
    }
    IEnumerator HidingLine(Color startColor, Color endColor)
    {
        while (line.startColor.a > 0 || line.endColor.a > 0)
        {
            line.startColor = new Color(startColor.r, startColor.g, startColor.b, line.startColor.a - Time.deltaTime * 4);
            line.endColor = new Color(endColor.r, endColor.g, endColor.b, line.endColor.a - Time.deltaTime * 4);
            Debug.Log("Hiding line... " + line.startColor.a + " - " + line.endColor.a);
            yield return null;
        }
    }
    
}
