using UnityEngine;

public class SwordSlash : MonoBehaviour
{
    [Header("斬擊Prefab")]
    public GameObject slashPrefab;
    [Header("生成位置")]
    public Transform spawnPoint;
    public AudioClip drawSword;
    public AudioClip slash;

    // 公開方法：在生成位置生成劍痕
    public void SpawnSlash()
    {
        if (slashPrefab != null && spawnPoint != null)
        {
           GameObject slash = Instantiate(slashPrefab, spawnPoint.position, spawnPoint.rotation);
           slash.SetActive(true);
        }
        else
        {
            Debug.LogWarning("slashPrefab 或 spawnPoint 未設定！");
        }
    }
    
    // 播放拔刀音效
    public void PlayDrawSwordSound()
    {
        if (drawSword != null)
        {
            var audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = drawSword;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("drawSword 音效未設定！");
        }
    }

    // 播放斬擊音效
    public void PlaySlashSound()
    {
        if (slash != null)
        {
            var audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = slash;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("slash 音效未設定！");
        }
    }
}
