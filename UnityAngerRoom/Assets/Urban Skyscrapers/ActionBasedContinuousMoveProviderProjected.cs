using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// מחליף את ה-Continuous Move של XRI ומקרין את התנועה על שיפוע הקרקע
public class ActionBasedContinuousMoveProviderProjected : ActionBasedContinuousMoveProvider
{
    GroundStickAndProject ground;

    protected override void Awake()
    {
        base.Awake();
        ground = GetComponent<GroundStickAndProject>(); // חייב להיות על אותו אובייקט (XR Rig)
    }

    // זה הפונקציה ש-XRI קורא כדי לחשב את וקטור התנועה בכל פריים
    protected override Vector3 ComputeDesiredMove(Vector2 input)
    {
        // נותן ל-XRI לחשב את הכיוון/מהירות הרגילים
        var move = base.ComputeDesiredMove(input);

        // מקרין את התנועה על משטח הקרקע (הנורמל האחרון שנמדד ע"י הסקריפט)
        if (ground != null)
            move = ground.ProjectOnGround(move);

        return move;
    }
}
