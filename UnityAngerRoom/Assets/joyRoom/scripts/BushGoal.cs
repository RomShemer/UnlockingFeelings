// Attach על כל שיח יעד
using UnityEngine;

public class BushGoal : MonoBehaviour
{
    [Tooltip("חייב להיות זהה ל-colorKey של הפרפר התואם")]
    public string colorKey;

    [Tooltip("לאן להצמיד את הפרפר כשהוא נכון (אפשר ריק, ואז נשתמש במיקום השיח)")]
    public Transform snapPoint;

    bool filled;

    void OnTriggerEnter(Collider other)
    {
        if (filled) return;

        var butterfly = other.GetComponentInParent<ButterflyId>();
        if (butterfly == null) return;

        if (butterfly.colorKey == colorKey)
        {
            // התאמה נכונה!
            if (snapPoint == null) snapPoint = transform;
            butterfly.FreezeAt(snapPoint);
            filled = true;
            MatchManager.Instance.ReportPlaced();
        }
    }
}