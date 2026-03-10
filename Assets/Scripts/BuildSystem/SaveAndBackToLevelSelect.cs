using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveAndBackToLevelSelect : MonoBehaviour
{
    [SerializeField] private EditorPlayRuntime runtime;
    [SerializeField] private string levelSelectSceneName = "LevelSelect";

    public void UI_SaveAndBack()
    {
        if (runtime == null)
        {
            Debug.LogError("SaveAndBackToLevelSelect: runtime is null");
            return;
        }

        runtime.UI_Save();
        SceneManager.LoadScene(levelSelectSceneName);
    }
}