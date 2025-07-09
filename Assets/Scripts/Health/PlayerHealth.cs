using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class PlayerHealth : BaseHealth
{
    private DialogueRunner dialogueRunner;

    [Header("畫面泛白用 Volume（需有 Bloom 覆蓋）")]
    public Volume volume;
    private Bloom bloom;

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;
        // gameObject.SetActive(false);
        dialogueRunner = FindObjectOfType<DialogueRunner>();
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
            dialogueRunner.StartDialogue("MissionFailed");
            PlayerController.EnableFire(false);
        }
    }

    private void OnDialogueComplete()
    {
        dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueComplete);
        StartCoroutine(BloomAndReload());
    }

    private IEnumerator BloomAndReload()
    {
        float duration = 2f;
        float timer = 0f;
        float from = 0f; // 初始值改為 0
        float to = 50f; // 目標值改為 50
        volume.profile.TryGet<Bloom>(out bloom);
        if (bloom != null)
        {
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float value = Mathf.Lerp(from, to, timer / duration);
                bloom.intensity.value = value;
                yield return null;
            }
            bloom.intensity.value = to;
        }
        else
        {
            Debug.Log("未取得 Bloom 組件");
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 你也可以加一個 public 方法方便外部調用
    public void SetBloomIntensity(float intensity)
    {
        if (bloom != null)
        {
            bloom.intensity.value = intensity;
        }
    }
} 