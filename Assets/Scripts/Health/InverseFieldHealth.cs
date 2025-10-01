using UnityEngine;
using System.Collections;

public class InverseFieldHealth : BaseHealth
{
    public static InverseFieldHealth instance { get; private set; }
    private LineRenderer lineRenderer;
    private float countdownDuration = 3f;
    public System.Action onTimeAdded;  // 當時間被加入時的事件
    [SerializeField] private TextMesh timeText;
    private float hideTextTimer = 0f;
    private const float TEXT_HIDE_DELAY = 0.5f;

    public void ResetTime()
    {
        currentCountdown = countdownDuration;
        hideTextTimer = 0f;
        if (timeText != null)
        {
            timeText.gameObject.SetActive(true);
            timeText.text = currentCountdown.ToString("F2");
        }
        onTimeAdded?.Invoke();
    }

    protected void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private Color normalColor = Color.blue;
    private Color hitColor = Color.white;
    private float normalWidth = 0.1f;
    private float hitWidth = 1f;
    private float effectTime = 0.2f;
    
    public float currentCountdown { get; private set; } = 0f;
    private bool isExpanding = false;
    private float expandDuration = 0.5f;
    private float currentExpandTime = 0f;
    private Vector3 originalScale;
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private BaseHealth playerHealth;
    [SerializeField] public BaseHealth TargetHealth;
    [SerializeField] public Transform MainWormHole;
    [SerializeField] private Collider[] playerColliders;

    private Collider m_Collider;

    protected void Start()
    {
        gameObject.tag = "Player";
        m_Collider = transform.GetComponent<Collider>();
        m_Collider.enabled = false;
        playerColliders = playerHealth.transform.GetComponentsInChildren<Collider>();
        // 記錄原始縮放值
        originalScale = transform.localScale;
        // 初始設為0
        transform.localScale = Vector3.zero;

        // 設置LineRenderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = normalWidth;
        lineRenderer.endWidth = normalWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = normalColor;
        lineRenderer.endColor = normalColor;
        lineRenderer.positionCount = 2;

        // 如果沒有指定TextMesh，就創建一個
        if (timeText == null)
        {
            GameObject textObj = new GameObject("TimeText");
            textObj.transform.SetParent(transform);
            textObj.transform.localPosition = Vector3.up * 0.5f; // 在物體上方0.5單位
            timeText = textObj.AddComponent<TextMesh>();
            timeText.alignment = TextAlignment.Center;
            timeText.anchor = TextAnchor.LowerCenter;
            timeText.fontSize = 50;
            timeText.characterSize = 0.1f;
        }
        timeText.gameObject.SetActive(false); // 一開始先隱藏
    }

    private void Update()
    {
        transform.position = playerHealth.transform.position;
        if (lineRenderer != null && MainWormHole != null)
        {
            // 根據碰撞體狀態設置LineRenderer的啟用狀態
            lineRenderer.enabled = m_Collider.enabled;
            
            if (lineRenderer.enabled)
            {
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, MainWormHole.position);
            }

            // 處理倒數計時
            if (currentCountdown > 0)
            {
                // 如果碰撞體還沒開啟，先開啟
                if (!m_Collider.enabled)
                {
                    m_Collider.enabled = true;
                    foreach (var collider in playerColliders)
                    {
                        collider.enabled = false;
                    }
                    StartCoroutine(PlayExpandAnimation());
                }

                // 倒數
                currentCountdown -= Time.deltaTime;
                if (currentCountdown <= 0)
                {
                    currentCountdown = 0;
                    m_Collider.enabled = false;
                    foreach (var collider in playerColliders)
                    {
                        collider.enabled = true;
                    }
                    StartCoroutine(PlayShrinkAnimation());
                }

                // 更新文字
                if (timeText != null)
                {
                    timeText.text = currentCountdown.ToString("F2");
                    timeText.gameObject.SetActive(true);
                }
                hideTextTimer = 0f;
            }
            else
            {
                // 處理文字隱藏
                if (timeText != null && timeText.gameObject.activeSelf)
                {
                    hideTextTimer += Time.deltaTime;
                    if (hideTextTimer >= TEXT_HIDE_DELAY)
                    {
                        timeText.gameObject.SetActive(false);
                    }
                }
            }

            // 處理展開/收縮動畫
            if (isExpanding)
            {
                currentExpandTime += Time.deltaTime;
                float t = Mathf.Clamp01(currentExpandTime / expandDuration);
                transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);

                if (t >= 1f)
                {
                    isExpanding = false;
                }
            }
        }
        else
        {
            Debug.LogError("LineRenderer or MainWormHole is null");
        }
    }


    private IEnumerator PlayExpandAnimation()
    {
        // 重置動畫狀態
        isExpanding = true;
        currentExpandTime = 0f;
        
        // 設置初始狀態
        transform.localScale = Vector3.zero;
        
        yield break;
    }

    private IEnumerator PlayShrinkAnimation()
    {
        float shrinkTime = 0f;
        Vector3 startScale = transform.localScale;

        while (shrinkTime < expandDuration)
        {
            shrinkTime += Time.deltaTime;
            float t = Mathf.Clamp01(shrinkTime / expandDuration);
            
            // 反向插值：從當前大小縮小到0
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            
            yield return null;
        }

        // 確保最後完全縮小到0
        transform.localScale = Vector3.zero;
    }

    public void SetTargetHealth(BaseHealth targetHealth)
    {
        TargetHealth = targetHealth;
    }


    public override void TakeDamage(float damage)
    {
        if (TargetHealth == null) return;

        // 對目標造成傷害
        TargetHealth.TakeDamage(damage * damageMultiplier);

        // 生成爆炸效果
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, MainWormHole.position, Quaternion.identity);
        }

        // 改變線條顏色
        StartCoroutine(ChangeColorEffect());
    }

    private IEnumerator ChangeColorEffect()
    {
        // 設置為白色且變粗
        lineRenderer.startColor = hitColor;
        lineRenderer.endColor = hitColor;
        lineRenderer.startWidth = hitWidth;
        lineRenderer.endWidth = hitWidth;

        // 等待指定時間
        yield return new WaitForSeconds(effectTime);

        // 恢復為原本的藍色和寬度
        lineRenderer.startColor = normalColor;
        lineRenderer.endColor = normalColor;
        lineRenderer.startWidth = normalWidth;
        lineRenderer.endWidth = normalWidth;
    }
}
