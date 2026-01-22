using System.Collections.Generic;
using UnityEngine;

public class MovablePlatform : MonoBehaviour
{
    [SerializeField] private PlatformMovementTypes type;

    [Header("PingPong Movement")]
    [SerializeField] private float horizontalSpeed = 5f;
    [SerializeField] private float horizontalDistance = 5f;
    [SerializeField] private float verticalSpeed = 5f;
    [SerializeField] private float verticalDistance = 5f;

    [Header("Custom Path Movement")]
    [SerializeField] private float customMoveDuration = 1.0f; 
    [SerializeField] private List<Transform> pathPoints = new List<Transform>();

    private readonly List<Vector3> destinations = new List<Vector3>();
    private int destinationIndex = 0;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private float timer = 0f;

    private void Start()
    {
        startPosition = transform.position;
        destinations.Clear();
        foreach (Transform t in pathPoints)
        {
            if (t != null) destinations.Add(t.position);
        }
        destinations.Add(transform.position);
        if (destinations.Count > 0)
        {
            endPosition = destinations[destinationIndex];
        }
        else
        {
            endPosition = transform.position;
        }
    }

    private void Update()
    {
        Move();
    }

    private void Move()
    {
        switch (type)
        {
            case PlatformMovementTypes.HORIZONTAL:
                {
                    float x = Mathf.PingPong(horizontalSpeed * Time.time, horizontalDistance) + startPosition.x;
                    transform.position = new Vector3(x, transform.position.y, transform.position.z);
                    break;
                }

            case PlatformMovementTypes.VERTICAL:
                {
                    float y = Mathf.PingPong(verticalSpeed * Time.time, verticalDistance) + startPosition.y;
                    transform.position = new Vector3(transform.position.x, y, transform.position.z);
                    break;
                }
            case PlatformMovementTypes.DIAGNAL_RIGHT:
                {
                    float x = Mathf.PingPong(horizontalSpeed * Time.time, horizontalDistance) + startPosition.x;
                    float y = Mathf.PingPong(verticalSpeed * Time.time, verticalDistance) + startPosition.y;
                    transform.position = new Vector3(x, y, transform.position.z);
                    break;
                }

            case PlatformMovementTypes.DIAGNAL_LEFT:
                {
                    float x = startPosition.x - Mathf.PingPong(horizontalSpeed * Time.time, horizontalDistance);
                    float y = Mathf.PingPong(verticalSpeed * Time.time, verticalDistance) + startPosition.y;
                    transform.position = new Vector3(x, y, transform.position.z);
                    break;
                }

            case PlatformMovementTypes.CUSTOM:
                {
                    if (destinations.Count == 0) return;

                    timer += Time.deltaTime / Mathf.Max(0.0001f, customMoveDuration);
                    transform.position = Vector3.Lerp(startPosition, endPosition, timer);

                    if (timer >= 1f)
                    {
                        timer = 0f;
                        destinationIndex++;
                        if (destinationIndex >= destinations.Count) destinationIndex = 0;

                        startPosition = endPosition;
                        endPosition = destinations[destinationIndex];
                    }
                    break;
                }
        }
    }
}
