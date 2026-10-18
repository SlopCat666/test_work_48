using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; } // синглтон для доступа из других систем
    [SerializeField] private float gainFactor = 0.5f; // коэффициент влияния на торможение
    public CreatureStats stats { get; private set; } // ссылка на характеристики существа

    [SerializeField] private Transform groundCheckPoint; // точка проверки земли (под ногами)
    [SerializeField] private float groundCheckDistance = 0.2f; // длина луча проверки земли

    [Header("Движение")]
    [SerializeField] private float accelRate = 40f; // м/с² — разгон
    [SerializeField] private float decelRate = 60f; // м/с² — торможение

    [Header("Поворот персонажа")]
    [SerializeField] private float rotationSpeed = 12f; // скорость поворота (Slerp-коэффициент)
    [SerializeField] private float mouseSensitivity = 2f; // градусов на единицу Mouse X

    private Vector3 moveDirection = Vector3.zero; // направление движения в плоскости XZ, y = 0
    private Rigidbody rb; // ссылка на физическое тело
    private Animator animator; // контроллер анимации
    private bool checkJump; // Переменная для контроля прыжка, чтобы не прыгать 10 раз менее, чем за секунду
    private Collider ownCollider; // Наша коллизия

    public CreatureStats getPlayerStats => stats; // быстрый доступ к характеристикам через игрока

    private static Action _onSpawned;
    public static event Action OnSpawned
    {
        add
        {
            _onSpawned += value;
            if (Instance != null) value?.Invoke(); // если игрок уже создан — сразу вызываем подписчика
        }
        remove
        {
            _onSpawned -= value;
        }
    }

    // "псевдопеременная": любое изменение moveDirection проходит через сеттер,
    // поэтому анимация и поворот модели синхронизируются автоматически — не нужно
    // помнить о них в каждом месте, где меняется движение.
    private Vector3 move
    {
        get => moveDirection;
        set
        {
            moveDirection = value;

            // В 2D в анимацию уходило Mathf.Abs(moveDirection) — движение вдоль одной оси.
            // В 3D длина вектора ввода уже в [0, 1], её и отдаём в параметр Move.
            animator.SetFloat("Move", Mathf.Clamp01(moveDirection.magnitude));
        }
    }

    private bool isGrounded // стоит ли на земле (по лучу)
    {
        get { return animator.GetBool("Ground"); }
        set { animator.SetBool("Ground",value); }
    }

    void Start()
    {
        // Защита от дубликатов при перезагрузке сцены / респавне
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // установка всех связей и значений
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        stats = GetComponent<CreatureStats>();
        ownCollider = GetComponent<Collider>();
        checkJump = false; // изначально прыжок "не заказан"

        _onSpawned?.Invoke(); // сообщаем всем, кто ждал спавна игрока

        if (GameManager.Instance==null)
        {
            Debug.Log("Менеджера игрой нет");
        }
    }


    private void FixedUpdate()
    {
        bool hasInput = moveDirection.sqrMagnitude > 0.01f;

        // Целевая горизонтальная скорость. Без ввода — ноль.
        Vector3 target = hasInput
            ? moveDirection.normalized * stats.MoveSpeed * Mathf.Clamp01(moveDirection.magnitude)
            : Vector3.zero;

        // Текущая горизонтальная скорость — из Rigidbody
        Vector3 current = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // Плавно ведём текущую к целевой. Разгон и торможение — разными темпами.
        float rate = hasInput ? accelRate : decelRate;
        Vector3 next = Vector3.MoveTowards(current, target, rate * Time.fixedDeltaTime);

        // Пишем обратно, Y не трогаем
        rb.linearVelocity = new Vector3(next.x, rb.linearVelocity.y, next.z);

        // Прыжок 
        if (checkJump)
        {
            rb.AddForce(Vector3.up * stats.JumpForce, ForceMode.Impulse);
            animator.Play("JumpStart");
            checkJump = false; // сбрасываем флаг — следующий прыжок только после нового нажатия
        }
    }

    void Update()
    {
        // контроль, что игра не остановлена
        if (Time.timeScale == 0) return;

        // --- Поворот тела мышью ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(Vector3.up, mouseX, Space.World);

        // Направление движения: 8 направлений в мире (WASD / стрелки).
        // ClampMagnitude: на диагонали |input| не должно быть √2 — иначе персонаж
        // на диагонали разгонялся бы быстрее, чем по прямой.
        // В 2D Input.GetAxis уже был нормирован, здесь это надо делать вручную.
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 input = Vector3.ClampMagnitude((transform.forward*v+transform.right*h), 1f);

        move = input; // прогоняем через сеттер — он обновит анимацию и поворот

        animator.SetFloat("Velocity Y", rb.linearVelocity.y); // отдельный параметр для высоты (прыжок/падение)

        // Определение земли: любой нетриггерный коллайдер под ногами,
        // кроме собственного. Маска слоёв не нужна — "стоять можно на всём,
        // что физически существует и не является триггером".
        if (Physics.Raycast(groundCheckPoint.position, Vector3.down,
                            out RaycastHit hit, groundCheckDistance,
                            Physics.DefaultRaycastLayers,      // все слои, кроме "Ignore Raycast"
                            QueryTriggerInteraction.Ignore)    // триггеры не считаются землёй
            && hit.collider != ownCollider)                 // и не свой же коллайдер
        {     
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        // Проверка кнопки прыжка:
        // 1. Нажат Space именно в этом кадре (GetKeyDown, а не GetKey)
        // 2. Стоим на земле (по лучу выше)
        // 3. Флаг checkJump ещё не выставлен — значит прыжок за текущее приземление
        //    ещё не "заказан", и мы не нажмём его повторно
        // Сам импульс применяется в FixedUpdate — так он гарантированно попадает
        // в физический шаг, и повторное срабатывание в том же кадре исключено.
        if (Input.GetKeyDown(KeyCode.Space) && // Проверка кнопки прыжка
            isGrounded &&                      // проверка земли
            !checkJump)                        // контроль, чтобы мы не "прыгали" множество раз в моменте
        {
            checkJump = true; // "заказываем" прыжок для FixedUpdate
        }
    }
}