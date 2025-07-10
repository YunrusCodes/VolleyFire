public interface IHealth
{
    float GetCurrentHealth();
    float GetMaxHealth();
    bool IsDead();
    void TakeDamage(float damage);
    void SetMaxHealth(float newMaxHealth);
} 