using UnityEngine;
using UnityEngine.UI;

public class WarningTextTracker : MonoBehaviour
{
    public LineRenderer targetLine;
    public Canvas targetCanvas;
    private RectTransform rectTransform;
    private float lineLength;
    private Text text;

    [Header("閃爍設置")]
    public float blinkInterval = 0.2f;  // 閃爍間隔時間
    private float blinkTimer;
    private bool isVisible = true;
    private Color originalColor;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        text = GetComponent<Text>();
        if (text != null)
        {
            originalColor = text.color;
        }
    }

    void Update()
    {
        if (targetLine == null || targetCanvas == null)
        {
            // 如果目標消失，銷毀自己
            Destroy(gameObject);
            return;
        }

        // 取得線的起點和方向
        Vector3 startPoint = targetLine.GetPosition(0);
        Vector3 endPoint = targetLine.GetPosition(1);
        Vector3 direction = (endPoint - startPoint).normalized;

        // 計算與 z=0 平面的交點
        float t = -startPoint.z / direction.z;
        Vector3 intersectionPoint = startPoint + direction * t;

        // 更新UI位置
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(intersectionPoint);
        Vector2 canvasPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetCanvas.GetComponent<RectTransform>(),
            screenPoint,
            targetCanvas.worldCamera,
            out canvasPosition
        );
        rectTransform.anchoredPosition = canvasPosition;

        // 處理閃爍
        if (text != null)
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkInterval)
            {
                blinkTimer = 0f;
                isVisible = !isVisible;
                text.color = isVisible ? originalColor : new Color(originalColor.r, originalColor.g, originalColor.b, 0);
            }
        }
    }
}
