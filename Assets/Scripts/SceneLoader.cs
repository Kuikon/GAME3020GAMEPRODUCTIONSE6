using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
     private string sceneName;
    public void SceneLoad(string  sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
