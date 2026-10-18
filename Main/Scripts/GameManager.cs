using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    // при первом обращении либо находит GameManager на сцене,
    // либо создаёт его сам. Это убирает необходимость вручную расставлять объект.
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Уже есть на сцене? Найдём.
                _instance = FindFirstObjectByType<GameManager>();

                // Нет — создаём новый объект с этим компонентом.
                if (_instance == null)
                {
                    var go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                    Debug.Log("[GameManager] Не найден на сцене — создан автоматически.");
                }
            }
            return _instance;
        }
    }

    // Здесь будем хранить номер текущего уровня
    public int currentLevel { get; private set; }

    public bool gameOver { get; private set; } // обозначение поражения

    private void Awake()
    {
        // Защита от дубликатов при перезагрузке сцены
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        PlayerController.OnSpawned += OnPlayerSpawned;
    }

    private void OnPlayerSpawned()
    {
        Time.timeScale = 1;
        PlayerController.Instance.stats.OnDeath += GameOver;
    }
    void Start()
    {
        // Запоминаем номер сцены, в которой мы появились
        currentLevel = SceneManager.GetActiveScene().buildIndex;
        //Debug.Log("Текущий уровень: " + currentLevel);
        if (currentLevel == 0)
        {
            Time.timeScale = 1;
        }
    }

    // вызывается, когда достигаем финиша.
    public void LevelComplete()
    {
        Time.timeScale = 0;
        // Показываем панель победы (если есть) – можно добавить позже
        if (ControlUIPlayer.Instance != null)
        {
            ControlUIPlayer.Instance.ShowVictoryPanel();
        }
        else
        {
            //Определяем, есть ли следующий уровень
            int nextLevel = currentLevel + 1;
            if (nextLevel < SceneManager.sceneCountInBuildSettings)
            {
                // Следующий уровень существует – загружаем его
                LoadNextLevel(); // или LoadLevel(nextLevel);
            }
            else
            {
                LoadLevel(0);
            }
        }


    }

    /// Загружает уровень по его номеру (индексу в настройках сборки)
    /// levelNumber -> Номер уровня
    public void LoadLevel(int levelNumber)
    {
        currentLevel = levelNumber;          // запоминаем новый уровень
        gameOver = false;
        SceneManager.LoadScene(levelNumber); // загружаем сцену
        Time.timeScale = 1;
    }

    /// Загружает следующий уровень (текущий + 1)
    public void LoadNextLevel()
    {
        int nextLevel = currentLevel + 1;
        // Проверяем, не вышли ли за пределы (чтобы не упасть в ошибку)
        if (nextLevel < SceneManager.sceneCountInBuildSettings)
        {
            LoadLevel(nextLevel);
        }
        else
        {
            LoadLevel(0);
        }
    }

    /// Перезапускает текущий уровень, загружая его заново.
    public void RestartLevel()
    {
        // Загружаем текущий уровень, используя сохранённый индекс сцены
        LoadLevel(currentLevel);
    }

    /// Обрабатывает ситуацию уничтожения замка
    /// останавливает время и показывает интерфейс перезапуска.
    private void GameOver()
    {
        // Устанавливаем значение поражения
        gameOver = true;
        // Останавливаем игровое время, чтобы всё замерло
        Time.timeScale = 0;
        // рестарт игры
        RestartLevel();
    }

    public void TimeStopPlay(bool s)
    {
        Time.timeScale = s ? 0 : 1;
    }

    /// Выход из игры
    public void QuitGame()
    {
        Debug.Log("Выход из игры");
        Application.Quit();

        // Для редактора (чтобы можно было проверить)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
