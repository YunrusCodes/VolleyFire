using UnityEngine;

public class FunnelHealth : BaseHealth
{
    public Transform crashEffect;

    protected override void Die()
    {
        if (isDead) return;
        
        // 呼叫基底類別的 Die 方法
        base.Die();

        // 啟用 CrashEffect
        if (crashEffect != null)
        {
            crashEffect.gameObject.SetActive(true);
            StartCoroutine(MoveCrashEffectDown());
        }
    }

    private System.Collections.IEnumerator MoveCrashEffectDown()
    {
        float duration = 10f;
        float elapsed = 0f;
        float fallSpeed = 6f; // 初始下墜速度
        float acceleration = 9.8f; // 每秒下墜加速度
        float currentSpeed = fallSpeed;
        float rotateSpeed = 90f; // 每秒繞 x 軸旋轉 1 度
        while (elapsed < duration)
        {
            transform.position += Vector3.down * currentSpeed * Time.deltaTime;
            currentSpeed += acceleration * Time.deltaTime;

            if(elapsed < 1) transform.Rotate(Vector3.right, rotateSpeed * Time.deltaTime, Space.Self);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Destroy(gameObject);
    }
} 