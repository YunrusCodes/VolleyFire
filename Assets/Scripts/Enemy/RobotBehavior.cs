using UnityEngine;
public class RobotBehavior : EnemyBehavior
{
    public enum RobotMode { Idle = 0, GunMode = 1, SwordMode = 2 }

    [Header("Mode Settings")]
    public RobotMode mode = RobotMode.GunMode;

    // ──────────────────────────────────────────────────────────────
    private EnemyController controller;
    private Animator animator;
    private bool slashing = false;
    private bool drawshooting = false;

    public bool slashbool = false;
    public bool sheathbool = false;
    public bool drawshootbool = false;

    // ──────────────────────────────────────────────────────────────
    #region Init
    public override void Init(EnemyController controller)
    {
        this.controller = controller;
        animator = controller.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator not found on Robot!");
            enabled = false;
            return;
        }

        ResetState();
    }

    private void ResetState()
    {
        slashing = false;
        drawshooting = false;
        slashbool = false;
        sheathbool = false;
        drawshootbool = false;
    }
    #endregion

    // ──────────────────────────────────────────────────────────────
    #region Tick
    public override void Tick()
    {
        if (animator == null) return;

        switch (mode)
        {
            case RobotMode.Idle:
                HandleIdleMode();
                break;
            case RobotMode.GunMode:
                HandleGunMode();
                break;
            case RobotMode.SwordMode:
                HandleSwordMode();
                break;
        }
        
    }
    #endregion

    // ──────────────────────────────────────────────────────────────
    #region Mode Handlers
    private void HandleIdleMode()
    {
        AnimatorStateInfo IdleLayer = animator.GetCurrentAnimatorStateInfo(0);
        if (IdleLayer.IsName("Idle"))
        {
            ResetState();
        }
    }

    private void HandleGunMode()
    {
        if (sheathbool)
        {
            animator.SetBool("DrawingGun", false);
            sheathbool = false;
            mode = 0;
        }
        else if (!animator.GetBool("DrawingGun"))
        {
            animator.SetBool("DrawingGun", true);
        }
    }

    private void HandleSwordMode()
    {
        AnimatorStateInfo SlashLayer = animator.GetCurrentAnimatorStateInfo(1);

        if (drawshootbool && !slashing)
        {
            animator.SetTrigger("DrawAndShoot");
            drawshootbool = false;
            drawshooting = true;
        }
        else if (drawshooting)
        {
            if (SlashLayer.normalizedTime >= 1f)
            {
                drawshooting = false;
            }
        }

        if (SlashLayer.IsName("Slash"))
        {
            slashing = true;
        }
        else if (slashing)
        {
            slashing = false;
        }
        else if (slashbool && !drawshooting)
        {
            animator.SetTrigger("Slash");
            slashbool = false;
        }
        else if (sheathbool)
        {
            animator.SetBool("DrawingSword", false);
            sheathbool = false;
            mode = 0;
        }
        else if (!animator.GetBool("DrawingSword"))
        {
            animator.SetBool("DrawingSword", true);
        }
    }
    #endregion
}
