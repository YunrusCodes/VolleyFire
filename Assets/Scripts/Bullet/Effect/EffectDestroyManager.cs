using UnityEngine;
using System.Collections;

public class EffectDestroyManager : MonoBehaviour
{
    private static EffectDestroyManager _instance;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public static void RegisterDestroy(GameObject obj, float delay)
    {
        Debug.Log($"[EffectDestroyManager] RegisterDestroy called for {obj.name}, delay={delay}");
        if (_instance == null)
        {
            // 若場上沒有 EffectDestroyManager，自動建立一個
            GameObject go = new GameObject("EffectDestroyManager");
            _instance = go.AddComponent<EffectDestroyManager>();
            DontDestroyOnLoad(go);
        }
        _instance.StartCoroutine(_instance.DelayDestroy(obj, delay));
    }

    private IEnumerator DelayDestroy(GameObject obj, float delay)
    {
        Debug.Log($"[EffectDestroyManager] DelayDestroy start for {obj.name}, delay={delay}");
        yield return new WaitForSeconds(delay);
        if (obj != null)
        {
            Debug.Log($"[EffectDestroyManager] Destroying {obj.name}");
            Destroy(obj);
        }
    }
} 