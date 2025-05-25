using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

// Базовый класс для управления движением персонажа по алгоритму
public class BaseMovementController : MonoBehaviour
{
    [Header("Base Settings")]
    [SerializeField] protected InputField algorithmText; // Текстовое поле для отображения алгоритма
    [SerializeField] protected float moveSpeed = 100f; // Скорость движения персонажа
    [SerializeField] protected LayerMask obstacleLayer; // Слой для объектов, которые блокируют движение

    [Header("Animation Settings")]
    public Animator Ivan_animator;

    [Header("Windows")]
    [SerializeField] protected GameObject DialogeWindowStory;
    [SerializeField] protected GameObject DialogeWindowGoodEnd; // Диалоговое окно для прохождения
    [SerializeField] protected GameObject DialogeWindowBadEnd; // Диалоговое окно для проигрыша
    [SerializeField] protected GameObject DialogeWindowError; // Диалоговое окно ошибки
    [SerializeField] private GameObject uiCanvasObject;

    [Header("Check Points")]
    [SerializeField] protected List<Transform> checkPoints; // Список всех чекпоинтов

    protected List<string> algorithmSteps = new List<string>(); // Список шагов алгоритма
    protected bool isPlaying = false; // Флаг для проверки, проигрывается ли алгоритм
    protected bool isPathBlocked = false; // Флаг для проверки, заблокирован ли путь
    protected Transform playerTransform;  // Ссылка на игрока
    protected Transform currentCheckPoint; // Текущий чекпоинт

    //Для сбора предметов
    protected List<GameObject> itemsToCollect = new List<GameObject>();// Предметы для сбора
    protected List<Vector3> itemOriginalPositions = new List<Vector3>(); // Исходные позиции предметов
    protected List<bool> itemActiveStates = new List<bool>(); // Исходное состояние предметов (взят или нет)
    protected int collectedItemsCount = 0; // Счетчик собранных предметов
    protected bool allItemsCollected = false; // Флаг полного сбора

    // Элементы интерфейса для прокрутки
    protected ScrollRect scrollRect;
    protected RectTransform scrollRectTransform;
    protected RectTransform textRectTransform;

    private GameObject currentActiveWindow;
    protected string currentWindowName;
    protected string[] storyMessages = new string[0]; // Массив сообщений для окна истории
    protected string[] goodEndMessages = new string[0]; // Массив сообщений для окна победы на 5 уровне
    protected int currentMessageIndex = 0; // Текущий индекс сообщения

    public LayerMask ObstacleLayer => obstacleLayer;
    public Canvas canvas;
    public AudioSource clickSound;
    public AudioSource GetSound;

    protected virtual int GetInitialCheckpointIndex() => 0;
    protected virtual int GetTargetCheckpointIndex() => 1;
    protected Transform targetCheckPoint;  // Финальная точка

    protected virtual void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform; // Поиск игрока по тегу

        // Настройка элементов прокрутки
        scrollRect = algorithmText.GetComponentInParent<ScrollRect>();
        scrollRectTransform = scrollRect.GetComponent<RectTransform>();
        textRectTransform = algorithmText.textComponent.GetComponent<RectTransform>();

        InitializeCheckPoints();

        UpdateAlgorithmText();
    }


    // Настройка точек маршрута
    protected virtual void InitializeCheckPoints()
    {
        currentCheckPoint = checkPoints[GetInitialCheckpointIndex()];
        playerTransform.position = currentCheckPoint.position;
        targetCheckPoint = checkPoints[GetTargetCheckpointIndex()];
    }

    // Добавление шага в алгоритм
    public virtual void AddStep(string step)
    {
        if (!isPlaying) // Только если алгоритм не выполняется
        {
            algorithmSteps.Add(step);
            UpdateAlgorithmText();
        }
    }

    // Обновление текстового поля с алгоритмом
    protected virtual void UpdateAlgorithmText()
    {
        algorithmText.text = ""; // Очистка поля

        // Построчное заполнение с нумерацией
        for (int i = 0; i < algorithmSteps.Count; i++)
        {
            string numberPadding; // отступ
            if (i < 9) numberPadding = "   ";
            else numberPadding = "  ";

            // Виртуальный метод для форматирования строки
            algorithmText.text += $"{i + 1}{numberPadding}{algorithmSteps[i]};" + "\n";
        }

        StartCoroutine(ScrollIfOverflow()); // Проверка прокрутки
        ValidateLineCount(); // Проверка количества строк
    }

    // Проверка максимального количества шагов
    protected virtual void ValidateLineCount()
    {
        // Базовая проверка (можно переопределить в наследниках)
        int lineCount = algorithmText.text.Split('\n').Length - 1;
        if (lineCount > 19)
        {
            ShowErrorDialog($"Превышено максимальное количество шагов (19).");
            algorithmText.text = algorithmText.text.Substring(0, algorithmText.text.Length - 1);
        }
    }

    // Автоматическая прокрутка при переполнении
    protected IEnumerator ScrollIfOverflow()
    {
        yield return null; // Ждем один кадр для обновления UI
        
        Canvas.ForceUpdateCanvases();
        
        // Получаем текущую позицию прокрутки перед обновлением
        float currentScrollPos = scrollRect.verticalNormalizedPosition;
        
        float textHeight = LayoutUtility.GetPreferredHeight(textRectTransform);
        float scrollRectHeight = scrollRectTransform.rect.height;
        
        // Если текст не помещается и пользователь не прокручивал вверх вручную
        if (textHeight > scrollRectHeight && currentScrollPos <= 0.01f)
        {
            // Мгновенная прокрутка вниз (без анимации)
            scrollRect.verticalNormalizedPosition = 0f;
        }
        
        // Если пользователь прокручивал вверх, сохраняем его позицию
        else if (currentScrollPos > 0.01f)
        {
            scrollRect.verticalNormalizedPosition = currentScrollPos;
        }
    }

    // Запуск выполнения алгоритма
    public virtual void PlayAlgorithm()
    {
        if (!isPlaying && algorithmSteps.Count > 0)
        {
            isPlaying = true;
            StartCoroutine(ExecuteAlgorithm());
        }

        clickSound.Play();
    }

    // Базовый метод выполнения алгоритма
    protected virtual IEnumerator ExecuteAlgorithm()
    {
        for (int i = 0; i < algorithmSteps.Count; i++)
        {
            string step = algorithmSteps[i];
            Vector3 direction = GetDirectionFromStep(step);

            if (direction != Vector3.zero) // Для шагов движения
            {
                Transform nextCheckPoint = FindNextCheckPoint(direction, currentCheckPoint);
                if (nextCheckPoint != null)
                {
                    Ivan_animator.SetBool("Move", true);
                    yield return StartCoroutine(MovePlayer(nextCheckPoint.position));
                    currentCheckPoint = nextCheckPoint;
                }
            }
            else if (step == "Взять") // Для команды сбора
            {
                ExecuteGetCommand();
            }
        }

        // Проверка условий завершения уровня
        CheckLevelCompletion();
        Ivan_animator.SetBool("Move", false);
        isPlaying = false;
    }

    // Функция командды подбора объектов
    protected virtual void ExecuteGetCommand()
    {
        // Получаем масштаб канваса
        float pickupDistance = 200f * canvas.scaleFactor; ;

        // Позиция игрока
        Vector2 playerPos = RectTransformUtility.WorldToScreenPoint(Camera.main, playerTransform.position);

        for (int i = 0; i < itemsToCollect.Count; i++)
        {
            GameObject item = itemsToCollect[i];

            if (item != null && item.activeSelf)
            {
                // Получение позиции предмета
                Vector2 itemPos = RectTransformUtility.WorldToScreenPoint(Camera.main, item.transform.position);

                // Проверка расстояния
                if (Vector2.Distance(playerPos, itemPos) < pickupDistance)
                {
                    GetSound.Play();
                    item.SetActive(false);
                    collectedItemsCount++;

                    if (collectedItemsCount == 4)
                    {
                        allItemsCollected = true;
                    }
                    break;
                }
            }
        }
    }

    // Находим следующий чекпоинт в заданном направлении
    protected Transform FindNextCheckPoint(Vector3 direction, Transform currentCheckPoint)
    {
        Transform nearestCheckPoint = null;
        float nearestDistance = Mathf.Infinity;

        foreach (var checkPoint in checkPoints)
        {
            Vector3 delta = checkPoint.position - currentCheckPoint.position;
            if (Vector3.Dot(delta.normalized, direction.normalized) > 0.9f)
            {
                float distance = Vector3.Distance(currentCheckPoint.position, checkPoint.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestCheckPoint = checkPoint;
                }
            }
        }

        return nearestCheckPoint;
    }

    //Перемещение игрока к цели
    protected IEnumerator MovePlayer(Vector3 targetPosition, Transform player = null)
    {
        if (player == null)
        {
            player = playerTransform;
        }

        while (Vector3.Distance(player.position, targetPosition) > 0.01f)
        {
            if (!isPlaying || isPathBlocked || (currentActiveWindow != null))
                yield break;

            player.position = Vector3.MoveTowards(
                player.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }
        player.position = targetPosition;
    }

    // Конвертация текстовой команды в вектор
    protected Vector3 GetDirectionFromStep(string step)
    {
        switch (step)
        {
            case "Вверх": return Vector3.up;
            case "Вниз": return Vector3.down;
            case "Влево": return Vector3.left;
            case "Вправо": return Vector3.right;
            default: return Vector3.zero;
        }
    }

    // Остановка выполнения алгоритма
    public virtual void StopAlgorithm()
    {
        isPlaying = false;
        StopAllCoroutines(); // Остановка всех корутин
        algorithmSteps.Clear(); // Очистка списка команд
        algorithmText.text = ""; // Очистка текстового поля

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f; // Сброс прокрутки

        Ivan_animator.SetBool("Move", false);
        InitializeCheckPoints();

        clickSound.Play();
    }

    // Инициализация системы сбора предметов
    protected virtual void InitializeItems(string itemTag = "Item")
    {
        GameObject[] itemObjects = GameObject.FindGameObjectsWithTag(itemTag);
        if (itemObjects.Length == 0) return;

        // Сохранение исходных состояний
        itemsToCollect = new List<GameObject>(itemObjects);
        foreach (var item in itemsToCollect)
        {
            itemOriginalPositions.Add(item.transform.position);
            itemActiveStates.Add(item.activeSelf);
        }
    }

    // Сброс предметов в исходное состояние
    protected virtual void ResetItems()
    {
        collectedItemsCount = 0;
        allItemsCollected = false;

        // Восстанавливаем все предметы
        for (int i = 0; i < itemsToCollect.Count; i++)
        {
            if (itemsToCollect[i] != null)
            {
                itemsToCollect[i].SetActive(itemActiveStates[i]);
                itemsToCollect[i].transform.position = itemOriginalPositions[i];
            }
        }
    }

    // Обработчики столкновений
    private void OnTriggerEnter2D(Collider2D collision) {
        if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            isPathBlocked = true;
            StopAlgorithm();
            ShowBadEndDialog($"Иван столкнулся с препятствием.");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            isPathBlocked = false;
        }
    }

    // Модифицированный метод создания окон
    protected void CreateWindow(GameObject window, string message = "")
    {
        if (currentActiveWindow != null)
        {
            Destroy(currentActiveWindow);
        }

        currentActiveWindow = Instantiate(window, uiCanvasObject.transform);

        // Установка сообщения если оно передано
        if (!string.IsNullOrEmpty(message))
        {
            InputField textField = currentActiveWindow.GetComponentInChildren<InputField>();
            if (textField != null)
            {
                textField.text = message;
                textField.lineType = InputField.LineType.MultiLineNewline;
                textField.textComponent.alignment = TextAnchor.MiddleCenter;
                textField.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
                textField.textComponent.resizeTextForBestFit = true;
                textField.textComponent.resizeTextMinSize = 42;
                textField.textComponent.resizeTextMaxSize = 50;
            }
        }
    }

    // Метод для обработки прогресса в истории
    public virtual void AdvanceStory()
    {
        if (currentWindowName == "story")
        {
            currentMessageIndex++;
            if (currentMessageIndex < storyMessages.Length)
            {
                // Обновляем текст следующим сообщением
                InputField textField = currentActiveWindow.GetComponentInChildren<InputField>();
                if (textField != null)
                {
                    textField.text = storyMessages[currentMessageIndex];
                }
            }
            else
            {
                // Конец истории - закрываем окно
                CloseCurrentWindow();
                currentMessageIndex = 0; // Сброс для следующего раза
            }
        }
        else if (currentWindowName == "complete")
        {
            // Особый обработчик для финального уровня
            if (SceneManager.GetActiveScene().buildIndex == 5) // Последний уровень
            {
                currentMessageIndex++;
                if (currentMessageIndex < goodEndMessages.Length)
                {
                    // Обновляем текст следующим сообщением
                    InputField textField = currentActiveWindow.GetComponentInChildren<InputField>();
                    if (textField != null)
                    {
                        textField.text = goodEndMessages[currentMessageIndex];
                    }
                }
                else
                {
                    // После последнего сообщения показываем другое окно
                    CloseCurrentWindow();
                    currentMessageIndex = 0;
                    ShowFinalDialog();
                }
            }
            else
            {
                // Для обычных уровней
                LoadNextScene();
            }
        }
    }

    protected void CloseCurrentWindow()
    {
        if (currentActiveWindow != null)
        {
            Destroy(currentActiveWindow);
            currentActiveWindow = null;
        }
    }

    // Модифицированный метод показа окна истории
    protected virtual void ShowStoryDialog()
    {
        if (storyMessages.Length > 0)
        {
            currentWindowName = "story";
            CreateWindow(DialogeWindowStory, storyMessages[0]);
            currentMessageIndex = 0;
        }
    }

    // Модифицированный метод показа окна победы
    protected virtual void ShowCompletionDialog(string message = "")
    {
        currentWindowName = "complete";
        SaveLoadManager.SaveProgress(SceneManager.GetActiveScene().name);
        if (goodEndMessages.Length > 0 && SceneManager.GetActiveScene().buildIndex == 5)
        {
            // Для последнего уровня последовательность сообщений
            CreateWindow(DialogeWindowGoodEnd, goodEndMessages[0]);
            currentMessageIndex = 0;
        }
        else
        {
            // Для остальных уровней одно сообщение
            CreateWindow(DialogeWindowGoodEnd, message);
        }
    }

    // Показ окна ошибки
    protected virtual void ShowFinalDialog() {}

    //Показ окна проигрыша
    protected virtual void ShowBadEndDialog(string message = "")
    {
        CreateWindow(DialogeWindowBadEnd, message);
    }

    // Показ окна ошибки
    protected virtual void ShowErrorDialog(string message = "")
    {
        CreateWindow(DialogeWindowError, message);
    }

    protected virtual void CheckLevelCompletion()
    {

        if (allItemsCollected && currentCheckPoint == targetCheckPoint)
        {
            ShowCompletionDialog();
        }
        else
        {
            ShowBadEndDialog();
        }
    }

    // Загрузка следующего уровня
    public void LoadNextScene()
    {
        int nextLevelIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextLevelIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextLevelIndex);
        }
    }

    // Удаление последнего шага
    public virtual void RemoveLastStep()
    {
        clickSound.Play();
        if (!isPlaying && algorithmSteps.Count > 0)
        {
            algorithmSteps.RemoveAt(algorithmSteps.Count - 1);
            UpdateAlgorithmText();
            StartCoroutine(ScrollIfOverflow());
        }
    }

    // Методы для кнопок
    public void AddUpStep() { clickSound.Play(); AddStep("Вверх"); }
    public void AddDownStep() { clickSound.Play(); AddStep("Вниз"); }
    public void AddLeftStep() { clickSound.Play(); AddStep("Влево"); }
    public void AddRightStep() { clickSound.Play(); AddStep("Вправо"); }
    public void AddGet() { clickSound.Play(); AddStep("Взять"); }
}