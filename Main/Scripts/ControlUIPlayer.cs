using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;

public class ControlUIPlayer : MonoBehaviour
{
    public static ControlUIPlayer Instance { get; private set; } // синглтон для доступа из других систем
    [SerializeField] private Image barHealth; // UI-полоска здоровья
    [SerializeField] private GameObject victoryPanel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Защита от дубликатов при перезагрузке сцены / респавне
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        PlayerController.OnSpawned += LoadPlayer;

        victoryPanel.SetActive(false);
    }

    private void LoadPlayer()
    {
        PlayerController.Instance.stats.OnHealthChanged += UpdateBarHealth; // подписываемся на изменение здоровья для UI
        UpdateBarHealth();
    }

    // Обновление бара здоровья, если он назначен в инспекторе
    private void UpdateBarHealth()
    {
        if (barHealth != null && PlayerController.Instance!=null)
        {
            barHealth.fillAmount = PlayerController.Instance.stats.currentHealth 
                / PlayerController.Instance.stats.MaxHealth; // fillAmount ∈ [0, 1]
        }
    }

    // показ окна победы
    public void ShowVictoryPanel()
    {
        victoryPanel.SetActive(true);
    }

    // выход из игры
    public void ClickExit()
    {
        GameManager.Instance.QuitGame();
    }

    // перезагрузка уровня
    public void ClickReastart()
    {
        GameManager.Instance.RestartLevel();
    }
}
