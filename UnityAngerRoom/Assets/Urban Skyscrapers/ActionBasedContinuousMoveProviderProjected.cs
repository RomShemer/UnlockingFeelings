using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// ����� �� �-Continuous Move �� XRI ������ �� ������ �� ����� �����
public class ActionBasedContinuousMoveProviderProjected : ActionBasedContinuousMoveProvider
{
    GroundStickAndProject ground;

    protected override void Awake()
    {
        base.Awake();
        ground = GetComponent<GroundStickAndProject>(); // ���� ����� �� ���� ������� (XR Rig)
    }

    // �� �������� �-XRI ���� ��� ���� �� ����� ������ ��� �����
    protected override Vector3 ComputeDesiredMove(Vector2 input)
    {
        // ���� �-XRI ���� �� ������/������ �������
        var move = base.ComputeDesiredMove(input);

        // ����� �� ������ �� ���� ����� (������ ������ ����� �"� �������)
        if (ground != null)
            move = ground.ProjectOnGround(move);

        return move;
    }
}
