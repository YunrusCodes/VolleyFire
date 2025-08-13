using UnityEngine;

public abstract class ControllableObject : BulletBehavior
{
    public enum ControlState
    {
        Uncontrolled,    // 未被控制
        Controlled,      // 控制中
        Released        // 被釋放
    }

    public ControlState currentState = ControlState.Uncontrolled;
    protected float originalSpeed;
    protected readonly float releaseSpeedMultiplier = 5f;
    
    protected override void BehaviorOnStart()
    {
        originalSpeed = speed;
        SetLifetime(-1f); // 初始設置無限生命週期
    }

    /// <summary>
    /// 設置控制狀態
    /// </summary>
    public virtual void SetControlState(ControlState newState)
    {
        currentState = newState;
        OnStateChanged(newState);
    }

    /// <summary>
    /// 狀態改變時的處理
    /// </summary>
    protected virtual void OnStateChanged(ControlState newState)
    {
        switch (newState)
        {
            case ControlState.Uncontrolled:
                OnUncontrolled();
                break;
            case ControlState.Controlled:
                OnControlled();
                break;
            case ControlState.Released:
                OnReleased();
                break;
        }
    }

    /// <summary>
    /// 基礎移動邏輯
    /// </summary>
    protected override void Move()
    {
        switch (currentState)
        {
            case ControlState.Uncontrolled:
                MoveUncontrolled();
                break;
            case ControlState.Controlled:
                MoveControlled();
                break;
            case ControlState.Released:
                MoveReleased();
                break;
        }
    }

    /// <summary>
    /// 未控制狀態的移動邏輯
    /// </summary>
    protected virtual void MoveUncontrolled()
    {
        speed = originalSpeed;
        transform.position += direction * speed * Time.deltaTime;
    }

    /// <summary>
    /// 控制狀態的移動邏輯
    /// </summary>
    protected virtual void MoveControlled()
    {
        // 控制中預設不移動
    }

    /// <summary>
    /// 釋放狀態的移動邏輯
    /// </summary>
    protected virtual void MoveReleased()
    {
        direction = Vector3.forward;
        speed = originalSpeed * releaseSpeedMultiplier;
        transform.position += direction * speed * Time.deltaTime;
    }

    /// <summary>
    /// 進入未控制狀態時的處理
    /// </summary>
    protected virtual void OnUncontrolled() { }

    /// <summary>
    /// 進入控制狀態時的處理
    /// </summary>
    protected virtual void OnControlled() 
    {
        SetLifetime(-1f); // 控制時設置無限生命週期
    }

    /// <summary>
    /// 進入釋放狀態時的處理
    /// </summary>
    protected virtual void OnReleased() 
    {
        spawnTime = Time.time;
        SetLifetime(5f); // 釋放時設置 5 秒存活時間
    }

    /// <summary>
    /// 獲取當前控制狀態
    /// </summary>
    public ControlState GetControlState()
    {
        return currentState;
    }
}