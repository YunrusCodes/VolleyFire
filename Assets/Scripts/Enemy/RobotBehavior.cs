using UnityEngine;

public class RobotBehavior : EnemyBehavior
{
    public int mode = 1; // 1: DrawAndShoot, 2: DrawingSword+Slash, 3: DrawingSword+Slash+DrawAndShoot

    public GameObject sword; // 新增sword物件
    private EnemyController controller;
    private Animator animator;
    private float slashTimer = 0f;
    private float slashInterval = 3f;
    private bool swordSet = false;

    // 新增：追蹤Slash後是否要觸發DrawAndShoot
    private bool waitDrawAndShoot = false;
    private float drawAndShootDelay = 1f;
    private float drawAndShootTimer = 0f;

    // 新增：mode 2/3 duration
    public float mode2Duration = 5f;
    public float mode3Duration = 5f;
    private float mode2Timer = 0f;
    private float mode3Timer = 0f;
    private bool swordUnset2 = false;
    private bool swordUnset3 = false;

    public override void Init(EnemyController controller)
    {
        this.controller = controller;
        animator = controller.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator not found on Robot!");
            return;
        }
        slashTimer = 0f;
        swordSet = false;
        waitDrawAndShoot = false;
        drawAndShootTimer = 0f;
        mode2Timer = 0f;
        mode3Timer = 0f;
        swordUnset2 = false;
        swordUnset3 = false;
        // 可選：初始化時先關閉sword
        if (sword != null) sword.SetActive(false);
    }

    // 將設置DrawingSword=true與協程包成一個函式
    private void SetDrawingSwordTrue()
    {
        animator.SetBool("DrawingSword", true);
        // 啟動協程，0.5秒後顯示sword
        if (controller != null && sword != null)
            controller.StartCoroutine(SetSwordActiveWithDelay(0.5f));
    }

    // 將設置DrawingSword=false與協程包成一個函式
    private void SetDrawingSwordFalse()
    {
        animator.SetBool("DrawingSword", false);
        // 啟動協程，0.3秒後隱藏sword
        if (controller != null && sword != null)
            controller.StartCoroutine(SetSwordInactiveWithDelay(0.5f));
    }

    // 協程：延遲一段時間後顯示sword
    private System.Collections.IEnumerator SetSwordActiveWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        sword.SetActive(true);
    }

    // 協程：延遲一段時間後隱藏sword
    private System.Collections.IEnumerator SetSwordInactiveWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        sword.SetActive(false);
    }

    public override void Tick()
    {
        if (animator == null) return;

        slashTimer += Time.deltaTime;

        if (mode == 1)
        {
            if (slashTimer >= slashInterval)
            {
                animator.SetTrigger("DrawAndShoot");
                slashTimer = 0f;
            }
        }
        else if (mode == 2)
        {
            if (!swordSet)
            {
                SetDrawingSwordTrue();
                swordSet = true;
            }
            mode2Timer += Time.deltaTime;
            if (!swordUnset2 && mode2Timer >= mode2Duration)
            {
                SetDrawingSwordFalse();
                swordUnset2 = true;
            }
            if (slashTimer >= slashInterval)
            {
                animator.SetTrigger("Slash");
                slashTimer = 0f;
            }
        }
        else if (mode == 3)
        {
            if (!swordSet)
            {
                SetDrawingSwordTrue();
                swordSet = true;
            }
            mode3Timer += Time.deltaTime;
            if (!swordUnset3 && mode3Timer >= mode3Duration)
            {
                SetDrawingSwordFalse();
                swordUnset3 = true;
            }
            if (waitDrawAndShoot)
            {
                drawAndShootTimer += Time.deltaTime;
                // 只有在還沒超過duration時才觸發DrawAndShoot
                if (!swordUnset3 && drawAndShootTimer >= drawAndShootDelay)
                {
                    animator.SetTrigger("DrawAndShoot");
                    waitDrawAndShoot = false;
                    drawAndShootTimer = 0f;
                }
                // 如果已經超過duration，則不觸發DrawAndShoot，直接重置flag
                else if (swordUnset3)
                {
                    waitDrawAndShoot = false;
                    drawAndShootTimer = 0f;
                }
            }
            if (slashTimer >= slashInterval)
            {
                animator.SetTrigger("Slash");
                slashTimer = 0f;
                // 啟動延遲觸發DrawAndShoot
                waitDrawAndShoot = true;
                drawAndShootTimer = 0f;
            }
        }
    }
}
