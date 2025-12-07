using TMPro;
using UnityEngine;

public class LobbyDisplayController : MonoBehaviour
{
    [SerializeField] private GameObject singlePersonIcon;
    [SerializeField] private GameObject manyPersonIcon;
    
    [SerializeField] private TextMeshProUGUI lobbyName;
    [SerializeField] private TextMeshProUGUI peopleNum;

    public void SetName(string newName)
    {
        lobbyName.text = newName;
    }
    public void SetNum(int players)
    {
        peopleNum.text = $"{players} / 2";
        SetIcon(players);
    }
    private void SetIcon(int players)
    {
        if (players < 2)
        {
            singlePersonIcon.SetActive(true);
            manyPersonIcon.SetActive(false);
        }
        else
        {
            singlePersonIcon.SetActive(false);
            manyPersonIcon.SetActive(true);
        }
    }
}
