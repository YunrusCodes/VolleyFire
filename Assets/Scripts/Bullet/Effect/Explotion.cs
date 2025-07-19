using UnityEngine;
using System.Collections;
// 引入 EffectDestroyManager

public class Explotion : MonoBehaviour, EffectBehavior
{
    public float destroyDelay = 2f;

    private void OnEnable()
    {
        DestroySelf();
    }

    public void DestroySelf()
    {
        Debug.Log($"[Explotion] DestroySelf called on {gameObject.name}");
        EffectDestroyManager.RegisterDestroy(gameObject, destroyDelay);
    }
} 