using UnityEngine;
public class TempDoorTest : MonoBehaviour
{
    public DoorOpener opener;

    void Start()
    {
        opener?.Open();
        Debug.Log("[MatchProgress] All goals met -> opening door", this);

    } // יפתח אוטומטית ב-Play (גם ב-Quest)
}