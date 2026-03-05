using UnityEngine;

public enum StartMode
{
    Edit,
    Play
}

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    [Header("Current selection")]
    public string CurrentLevelId;
    public StartMode StartMode = StartMode.Edit;

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }
}