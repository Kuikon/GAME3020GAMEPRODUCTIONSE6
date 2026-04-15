using System;
using System.Collections;
using UnityEngine;

public class DroneRespawnCarrier : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private RobotControllerCommander followRobot;
    [SerializeField] private bool hoverAboveRobotWhenIdle = true;
    [SerializeField] private Vector3 idleHoverOffset = new Vector3(0f, 2.5f, 0f);
    [SerializeField] private float idleFollowSpeed = 5f;
    [SerializeField] private float idleRotateSpeed = 6f;

    [Header("Flight")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotateSpeed = 8f;
    [SerializeField] private float arriveDistance = 0.15f;

    [Header("Offsets")]
    [SerializeField] private Vector3 pickupHoverOffset = new Vector3(0f, 2.0f, 0f);
    [SerializeField] private Vector3 carryOffset = new Vector3(0f, -1.2f, 0f);
    [SerializeField] private Vector3 dropHoverOffset = new Vector3(0f, 2.0f, 0f);

    [Header("Refs")]
    [SerializeField] private Transform carryAnchor;

    [Header("Robot Grab Visual")]
    [SerializeField] private GameObject robotGrabbedChildObject;
    [SerializeField] private bool disableGrabbedChildOnDrop = true;

    [Header("Timing")]
    [SerializeField] private float grabPause = 0.2f;
    [SerializeField] private float dropPause = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    public bool IsBusy { get; private set; }

    private Coroutine routine;

    private void Awake()
    {
        if (followRobot == null)
            followRobot = FindFirstObjectByType<RobotControllerCommander>();

        if (robotGrabbedChildObject != null)
            robotGrabbedChildObject.SetActive(false);
    }

    private void Update()
    {
        if (IsBusy)
            return;

        if (!hoverAboveRobotWhenIdle)
            return;

        if (followRobot == null)
            return;

        Vector3 targetPos = followRobot.transform.position + idleHoverOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            idleFollowSpeed * Time.deltaTime);

        Vector3 toRobot = followRobot.transform.position - transform.position;
        toRobot.y = 0f;

        if (toRobot.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(toRobot.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                idleRotateSpeed * Time.deltaTime);
        }
    }

    public void StartCarryRespawn(
        RobotControllerCommander robot,
        Vector3 respawnWorldPos,
        Action onFinished)
    {
        if (robot == null)
        {
            Debug.LogWarning("[DroneRespawnCarrier] Robot is null.");
            return;
        }

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(CoCarryRespawn(robot, respawnWorldPos, onFinished));
    }

    private IEnumerator CoCarryRespawn(
        RobotControllerCommander robot,
        Vector3 respawnWorldPos,
        Action onFinished)
    {
        IsBusy = true;

        Rigidbody robotRb = robot.GetComponent<Rigidbody>();
        Collider[] robotColliders = robot.GetComponentsInChildren<Collider>(true);

        bool oldKinematic = false;
        RigidbodyInterpolation oldInterpolation = RigidbodyInterpolation.None;
        CollisionDetectionMode oldCollisionMode = CollisionDetectionMode.Discrete;

        if (robotRb != null)
        {
            oldKinematic = robotRb.isKinematic;
            oldInterpolation = robotRb.interpolation;
            oldCollisionMode = robotRb.collisionDetectionMode;
        }

        Vector3 robotPickupWorld = robot.transform.position;
        Vector3 pickupHoverWorld = robotPickupWorld + pickupHoverOffset;

        // 1) Fly above dead robot
        yield return MoveDroneTo(pickupHoverWorld);

        // 2) Grab robot
        yield return new WaitForSeconds(grabPause);

        if (debugLogs)
            Debug.Log("[DroneRespawnCarrier] Grab robot.");

        if (robotGrabbedChildObject != null)
            robotGrabbedChildObject.SetActive(true);

        if (robotRb != null)
        {
            robotRb.linearVelocity = Vector3.zero;
            robotRb.angularVelocity = Vector3.zero;
            robotRb.isKinematic = true;
            robotRb.interpolation = RigidbodyInterpolation.None;
            robotRb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }

        foreach (var col in robotColliders)
        {
            if (col != null)
                col.enabled = false;
        }

        Transform anchor = carryAnchor != null ? carryAnchor : transform;
        robot.transform.SetParent(anchor, true);
        robot.transform.localPosition = carryOffset;

        // 3) Fly to respawn point
        Vector3 dropHoverWorld = respawnWorldPos + dropHoverOffset;
        yield return MoveDroneTo(dropHoverWorld);

        // 4) Drop robot at respawn point
        robot.transform.SetParent(null, true);
        robot.transform.position = respawnWorldPos;

        yield return new WaitForSeconds(dropPause);

        foreach (var col in robotColliders)
        {
            if (col != null)
                col.enabled = true;
        }

        if (robotRb != null)
        {
            robotRb.isKinematic = oldKinematic;
            robotRb.interpolation = oldInterpolation;
            robotRb.collisionDetectionMode = oldCollisionMode;
            robotRb.linearVelocity = Vector3.zero;
            robotRb.angularVelocity = Vector3.zero;
        }

        if (disableGrabbedChildOnDrop && robotGrabbedChildObject != null)
            robotGrabbedChildObject.SetActive(false);

        if (debugLogs)
            Debug.Log("[DroneRespawnCarrier] Drop robot at respawn.");

        onFinished?.Invoke();

        IsBusy = false;
        routine = null;
    }

    private IEnumerator MoveDroneTo(Vector3 targetPos)
    {
        while ((transform.position - targetPos).sqrMagnitude > arriveDistance * arriveDistance)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                moveSpeed * Time.deltaTime);

            Vector3 toTarget = targetPos - transform.position;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    rotateSpeed * Time.deltaTime);
            }

            yield return null;
        }

        transform.position = targetPos;
    }
}