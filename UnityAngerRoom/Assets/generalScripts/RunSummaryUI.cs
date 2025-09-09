using UnityEngine;
using TMPro;

public class RunSummaryUI : MonoBehaviour
{
    [Header("Texts")]
    public TMP_Text headline;      // ����� ������ (���������)
    public TMP_Text achievements;  // ���� �"������"
    public TMP_Text challenges;    // ���� �"��������"
    public TMP_Text progress;      // ���� �"������"

    void OnEnable()
    {
        var rs = RunStats.Instance;
        if (rs == null)
        {
            if (headline) headline.text = "No stats available";
            if (achievements) achievements.text = "";
            if (challenges) challenges.text = "";
            if (progress) progress.text = "";
            return;
        }

        if (headline) headline.text = rs.BuildHeadline();
        if (achievements) achievements.text = rs.BuildAchievementsText();
        if (challenges) challenges.text = rs.BuildChallengesText();
        if (progress) progress.text = rs.BuildProgressText();

        Canvas.ForceUpdateCanvases();
    }

    // ������ ������ "Back to main menu"
    public void BackToMenu()
    {
        RoomRunManager.Instance?.LoadMainMenuAfterWinLoose();
    }

    // ���������: ������ ���� ��� ��� ����� ���
    public void NewGameDoorsMode()
    {
        RoomRunManager.Instance?.NewGameDoorsMode();
    }
}
