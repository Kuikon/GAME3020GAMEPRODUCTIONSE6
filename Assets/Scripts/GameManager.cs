using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    [Header("Current selection")]
    public string CurrentLevelId;

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }
}