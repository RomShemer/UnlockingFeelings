using TMPro;
using UnityEngine;

public class LoseRoomUIBinder : MonoBehaviour
{
    public TextMeshProUGUI achievementsText;
    public TextMeshProUGUI challengesText;
    public TextMeshProUGUI progressText;

    void Start()
    {
        if (RunStats.Instance == null)
        {
            if (achievementsText) achievementsText.text = "No data.";
            if (challengesText)   challengesText.text   = "No data.";
            if (progressText)     progressText.text     = "No data.";
            return;
        }

        if (achievementsText) achievementsText.text = RunStats.Instance.BuildAchievementsText();
        if (challengesText)   challengesText.text   = RunStats.Instance.BuildChallengesText();
        if (progressText)     progressText.text     = RunStats.Instance.BuildProgressText();
    }
}