using UnityEngine;
using System.Collections;

public class GaiaBehavior : EnemyBehavior
{
    public enum GaiaState
    {
        Idle,
        RocketPunch,
        RiderKick
    }

    protected GaiaState currentState = GaiaState.Idle;

    public override void Init(EnemyController controller)
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        
    }

    public override void Tick()
    {
        if (animator == null) return;

        bool next = animator.GetBool("Next");

        if (next)
        {
            // 狀態循環：Idle -> RiderKick -> RocketPunch -> Idle
            switch (currentState)
            {
                case GaiaState.Idle:
                    TransitionToState(GaiaState.RiderKick);
                    break;
                case GaiaState.RiderKick:
                    TransitionToState(GaiaState.RocketPunch);
                    break;
                case GaiaState.RocketPunch:
                    TransitionToState(GaiaState.Idle);
                    break;
            }
            animator.SetBool("Next", false);
        }
        else if(currentState == GaiaState.Idle)
        {
            animator.SetBool("Next", true);
        }
    }

    public virtual void TransitionToState(GaiaState newState)
    {
        // 如果狀態相同，不做改變
        if (currentState == newState) return;

        // 更新狀態
        currentState = newState;

        // 根據新狀態觸發對應動作
        switch (newState)
        {
            case GaiaState.Idle:
                OnIdleStart();
                break;
            case GaiaState.RocketPunch:
                OnRocketPunchStart();
                break;
            case GaiaState.RiderKick:
                RiderKickStart();
                break;
        }
    }

    // 各狀態開始時的方法
    void OnIdleStart()
    {
        animator.SetTrigger("Idle");
        animator.SetBool("CanAttack", true);
    }

    void OnRocketPunchStart()
    {
        animator.SetTrigger("PowerUp");
        animator.SetBool("CanAttack", true);
        animator.SetTrigger("RocketPunch");
    }

    void RiderKickStart()
    {
        // 觸發動畫
        animator.SetTrigger("JumpingJack");
        animator.SetBool("CanAttack", true);
    }
}