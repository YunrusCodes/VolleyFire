using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Gaia : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }
    [Header("特效生成點")]
    public Transform punchSpawnPoint;

    [Header("Mesh控制")]
    public List<GameObject> MeshObjects = new List<GameObject>();    // 需要控制顯示的物件列表

    public void HideMesh(bool hide)
    {
        foreach (GameObject obj in MeshObjects)
        {
            if (obj != null)
            {
                obj.SetActive(!hide);
            }
        }
    }

    [Header("火箭拳組件")]
    public GameObject Model_Punch;      // 手臂模型
    public GameObject Prefab_Punch;     // 火箭拳預製體

    [Header("騎士踢組件")]
    public GameObject Prefab_Kick;      // 踢擊預製體
    public GameObject Prefab_ReturnKick;      // 踢擊預製體


    // 騎士踢動畫事件
    public void RiderKicking()
    {
        if (animator != null)
        {
            animator.SetBool("CanTranslate", false);
        }

        if (Prefab_Kick != null)
        {
            GameObject kick = Instantiate(Prefab_Kick, transform.position, transform.rotation);
            HideMesh(true);
            // 尋找標記為Player的物件
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Vector3 targetDirection;
            
            if (player != null)
            {
                // 計算從踢擊到玩家的方向向量
                targetDirection = (player.transform.position - transform.position).normalized;
            }
            else
            {
                // 如果找不到玩家，使用預設的前向方向
                targetDirection = transform.forward;
                Debug.LogWarning("找不到標記為 Player 的物件，使用預設方向！");
            }
            
            StartCoroutine(KickMove(kick, targetDirection));
        }
        else
        {
            Debug.LogWarning("Model_Kick 或 Prefab_Kick 未設定！");
        }
    }

    Coroutine returnKickCoroutine = null;
    public void KickReturning()
    {
        if (returnKickCoroutine == null)
        {
            Vector3 spawnPosition = transform.position + transform.up * 50f;
            GameObject returnKick = Instantiate(Prefab_ReturnKick, spawnPosition, transform.rotation);
            returnKickCoroutine = StartCoroutine(KickReturn(returnKick));
        }
    }

    IEnumerator KickReturn(GameObject kick)
    {
        float duration = 1f;
        float timer = 0f;
        Vector3 startPosition = kick.transform.position;
        Vector3 targetPosition = transform.position;

        while (timer < duration && kick != null)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            kick.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        if (kick != null)
        {
            Destroy(kick);            
            HideMesh(false);
        }
        
        if (animator != null)
        {
            animator.SetBool("CanTranslate", true);
        }
        
        returnKickCoroutine = null;
    }

    IEnumerator KickMove(GameObject kick, Vector3 targetDirection)
    {
        float duration = 5f;
        float timer = 0f;
        float speed = 100f;
        
        // 使用 Lerp 平滑旋轉
        kick.transform.rotation = Quaternion.LookRotation(targetDirection.normalized);

        while (timer < duration && kick != null && kick.activeInHierarchy)
        {
            timer += Time.deltaTime;
            kick.transform.position += kick.transform.forward * speed * Time.deltaTime;            
            yield return null;
        }
        
        Destroy(kick);
        KickReturning();
    }

    // 火箭拳動畫事件
    public void RocketPunchFiring()
    {
        if (Model_Punch != null && Prefab_Punch != null)
        {
            // 隱藏手臂模型
            Model_Punch.SetActive(false);
            
            // 在手臂位置生成火箭拳
            Vector3 spawnPosition = Model_Punch.transform.position;
            Quaternion spawnRotation = Model_Punch.transform.rotation;
            GameObject punch = Instantiate(Prefab_Punch, spawnPosition, spawnRotation);
            // 尋找標記為Player的物件
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Vector3 targetDirection;
            
            if (player != null)
            {
                // 計算從火箭拳到玩家的方向向量
                targetDirection = (player.transform.position - Model_Punch.transform.position).normalized;
            }
            else
            {
                // 如果找不到玩家，使用預設的前向方向
                targetDirection = Model_Punch.transform.forward;
                Debug.LogWarning("找不到標記為 Player 的物件，使用預設方向！");
            }
            
            StartCoroutine(RocketPunchMove(punch, targetDirection));
        }
        else
        {
            Debug.LogWarning("Model_Punch 或 Prefab_Punch 未設定！");
        }
    }

    Coroutine returnPunchCoroutine = null;
    public void RocketPunchReturning()
    {
        if (!Model_Punch.activeSelf && returnPunchCoroutine == null)
        {
            Vector3 spawnPosition = Model_Punch.transform.position +  Model_Punch.transform.forward * 100f; // 在上方10單位處生成
            GameObject returnPunch = Instantiate(Prefab_Punch, spawnPosition, Model_Punch.transform.rotation);
            returnPunchCoroutine = StartCoroutine(RocketPunchReturn(returnPunch));
        }
    }
    
    IEnumerator RocketPunchReturn(GameObject punch)
    {
        float duration = 1f;
        float timer = 0f;
        Vector3 startPosition = punch.transform.position;
        Vector3 targetPosition = Model_Punch.transform.position;

        while (timer < duration && punch != null)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            punch.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            
            // 讓火箭拳朝向目標位置
            Vector3 direction = (targetPosition - punch.transform.position).normalized;
            
            yield return null;
        }

        if (punch != null)
        {
            Destroy(punch);
        }
        Model_Punch.SetActive(true);
        returnPunchCoroutine = null;
    }
    IEnumerator RocketPunchMove(GameObject punch, Vector3 targetDirection)
    {
        float duration = 5f;
        float timer = 0f;
        float speed = 100f;
              
        // 使用 Lerp 平滑旋轉
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection.normalized);
        while (timer < duration && punch != null && punch.activeInHierarchy)
        {
                          timer += Time.deltaTime;
              punch.transform.position += targetDirection.normalized * speed * Time.deltaTime;

              punch.transform.rotation = Quaternion.Lerp(punch.transform.rotation, targetRotation, Time.deltaTime * 0.05f);
              
              yield return null;
        }
        Destroy(punch);
    }
}