using UnityEngine;

public class TitleBGM : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SoundManager.Instance.PlayBGM(BGMSoundData.BGM.Title);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
