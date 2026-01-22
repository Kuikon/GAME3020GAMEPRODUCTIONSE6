using System.Collections;
using UnityEngine;

public class PipeWarp : MonoBehaviour
{
    [Header("Pipe")]
    [SerializeField] private Transform exitPoint;
    [SerializeField] private float cooldown = 0.4f;

    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";

    private bool isBusy;

    private void OnTriggerEnter(Collider other)
    {
        if (isBusy) return;
        if (!other.CompareTag(playerTag)) return;
        if (exitPoint == null) return;

        RobotController rc = other.GetComponent<RobotController>();
        Rigidbody rb = other.attachedRigidbody;

        if (rc == null || rb == null) return;

        StartCoroutine(WarpRoutine(other.transform, rb, rc));
    }

    private IEnumerator WarpRoutine(Transform player, Rigidbody rb, RobotController rc)
    {
        isBusy = true;

        // =============================
        // �� �������u�Ԃɑ����~
        // =============================
        rc.SetInputEnabled(false);

        // ��������U�~�߂�
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Trigger���d�h�~�̂���1�t���[���҂�
        yield return null;

        // =============================
        // ���[�v
        // =============================
        player.position = exitPoint.position;

        // ��������̂���1 FixedUpdate �҂�
        yield return new WaitForFixedUpdate();

        // =============================
        // �� ����ĊJ
        // =============================
        rc.SetInputEnabled(true);

        // �A�����[�v�h�~
        yield return new WaitForSeconds(cooldown);
        isBusy = false;
    }
}
