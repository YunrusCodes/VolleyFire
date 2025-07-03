public interface IHealth
{
    int GetCurrentHealth();
    int GetMaxHealth();
    bool IsDead();
    void TakeDamage(int damage);
    void SetMaxHealth(int newMaxHealth);
} 