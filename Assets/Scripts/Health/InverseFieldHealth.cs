using UnityEngine;
using System.Collections;

public class InverseFieldHealth : BaseHealth
{
    private LineRenderer lineRenderer;
    private Color normalColor = Color.blue;
    private Color hitColor = Color.white;
    private float normalWidth = 0.1f;
    private float hitWidth = 1f;
    private float effectTime = 0.2f;
    
    [SerializeField] private float countdownTime = 0f;
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



    protected void Start()
    {        
        // 尋找玩家物件
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            transform.SetParent(player.transform);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        playerColliders = playerHealth.transform.GetComponentsInChildren<Collider>();
        foreach (var collider in playerColliders)
        {
            collider.enabled = false;
        }

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
    }

    private void Update()
    {
        if (lineRenderer != null && MainWormHole != null)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, MainWormHole.position);

            // 處理倒數計時
            if (currentCountdown > 0)
            {
                currentCountdown -= Time.deltaTime;
                if (currentCountdown <= 0)
                {
                    currentCountdown = 0;
                    StartCoroutine(PlayShrinkAnimation());
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

    public void SetCountdownTime(float time)
    {
        // 如果從0變成非0
        if (currentCountdown <= 0 && time > 0)
        {
            StartCoroutine(PlayExpandAnimation());
        }
        // 如果從非0變成0
        else if (currentCountdown > 0 && time <= 0)
        {
            StartCoroutine(PlayShrinkAnimation());
        }
        currentCountdown = time;
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
