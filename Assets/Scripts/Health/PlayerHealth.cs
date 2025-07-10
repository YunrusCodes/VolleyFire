using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using UnityEngine.UI; // 新增 UI 命名空間

public class PlayerHealth : BaseHealth
{
    private DialogueRunner dialogueRunner;

    [Header("畫面泛白用 Volume（需有 Bloom 覆蓋）")]
    public Volume volume;
    private Bloom bloom;

    [Header("血條設置")]
    public Slider healthSlider;        // 血條滑桿
    public Image fillImage;            // 血條填充圖像
    public Color fullHealthColor = Color.green;     // 滿血顏色
    public Color lowHealthColor = Color.red;        // 低血顏色
    public float lowHealthPercent = 0.3f;          // 低血量百分比閾值

    private void Start()
    {
        // 初始化血條
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
            UpdateHealthBar();
        }
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthSlider == null || fillImage == null) return;

        healthSlider.value = currentHealth;
        
        // 計算血量百分比
        float healthPercent = currentHealth / maxHealth;
        
        // 根據血量百分比更新顏色
        fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent / lowHealthPercent);
    }

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
        float from = 0f;
        float to = 50f;
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

    public void SetBloomIntensity(float intensity)
    {
        if (bloom != null)
        {
            bloom.intensity.value = intensity;
        }
    }
} 