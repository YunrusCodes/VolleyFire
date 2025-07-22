using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("初始 BGM（可於 Inspector 指定）")]
    public AudioClip StartBgm;

    private AudioSource currentBgm;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 自動建立 AudioSource
        currentBgm = gameObject.AddComponent<AudioSource>();
        currentBgm.loop = true;
        if (StartBgm != null)
        {
            SwitchBGM(StartBgm);
        }
    }

    /// <summary>
    /// 切換 BGM，會自動停止前一首
    /// </summary>
    public void SwitchBGM(AudioClip newBgm)
    {
        // 若新舊BGM相同則不切換
        if (currentBgm.clip == newBgm)
        {
            return;
        }
        if (currentBgm.isPlaying)
        {
            currentBgm.Stop();
        }
        currentBgm.clip = newBgm;
        if (currentBgm.clip != null)
        {
            currentBgm.Play();
        }
    }
} 