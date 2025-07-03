using UnityEngine;
using System.Collections;

public class Explotion : MonoBehaviour, EffectBehavior
{
    public float destroyDelay = 2f;

    public void DestroySelf()
    {
        StartCoroutine(DelayDeactivate());
    }

    private IEnumerator DelayDeactivate()
    {
        yield return new WaitForSeconds(destroyDelay);        
        gameObject.SetActive(false); // 物件池用
        // 如果不用物件池，改成 Destroy(gameObject);
    }
} 