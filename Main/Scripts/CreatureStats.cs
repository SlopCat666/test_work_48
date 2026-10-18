using UnityEngine;
using System;

public class CreatureStats : MonoBehaviour
{
    // Стандартные характеристики существа
    [SerializeField]
    private float maxHealth = 100, // макс здоровье
        regenHp = 1, // Реген хп/c
        moveSpeed = 10, // скорость
        jumpForce = 5; // сила прыжка

    private float _currentHealth; // нынешнее здоровье

    // публичные параметры
    public float currentHealth// публичный параметр для взятия, но не изменений
    {
        get
        {
            return _currentHealth;
        }
        private set 
        {
            _currentHealth = Mathf.Clamp(value, -1f, maxHealth); // нет сверх здоровья

            OnHealthChanged?.Invoke();

            if (_currentHealth <= 0f)
            {
                OnDeath?.Invoke(); 
            }
        }
    }
    public float MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public float JumpForce => jumpForce;

    // События 
    public Action OnHealthChanged; // Изменение здоровья
    public Action OnDeath; // Смерть

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth; // устанавливаем здоровье
    }

    // Update is called once per frame
    void Update()
    {
        currentHealth += regenHp * Time.deltaTime; // регенерация
    }

    // функция урона, чтобы в будущем оставить пространство для манёвра
    public void Damage(float d)
    {
        if (d <= 0) return;
        currentHealth -= d;
    }

    // функция лечения, чтобы в будущем оставить пространство для манёвра
    public void Healing(float d)
    {
        if (d <= 0) return;
        currentHealth += d;
    }
}
