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

    [Header("Windows")]
    [SerializeField] protected GameObject DialogeWindowGoodEnd; // Диалоговое окно для прохождения
    [SerializeField] protected GameObject DialogeWindowBadEnd; // Диалоговое окно для проигрыша
    [SerializeField] protected GameObject DialogeWindowError; // Диалоговое окно ошибки

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

    protected virtual void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform; // Поиск игрока по тегу

        // Настройка элементов прокрутки
        scrollRect = algorithmText.GetComponentInParent<ScrollRect>();
        scrollRectTransform = scrollRect.GetComponent<RectTransform>();
        textRectTransform = algorithmText.textComponent.GetComponent<RectTransform>();

        UpdateAlgorithmText();
    }

    // Добавление шага в алгоритм
    public void AddStep(string step)
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
            // Виртуальный метод для форматирования строки
            algorithmText.text += FormatStepLine(i) + "\n";
        }

        StartCoroutine(ScrollIfOverflow()); // Проверка прокрутки
        ValidateLineCount(); // Проверка количества строк
    }

    // Форматирование строки с номером шага
    protected virtual string FormatStepLine(int index)
    {
        return $"{index + 1}   {algorithmSteps[index]};";
    }

    // Проверка максимального количества шагов
    protected virtual void ValidateLineCount()
    {
        // Базовая проверка (можно переопределить в наследниках)
        int lineCount = algorithmSteps.Count;
        if (lineCount > 20)
        {
            ShowErrorDialog($"Превышено максимальное количество шагов (20).");
        }
    }

    // Автоматическая прокрутка при переполнении
    protected virtual IEnumerator ScrollIfOverflow()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        float textHeight = LayoutUtility.GetPreferredHeight(textRectTransform);
        if (textHeight > scrollRectTransform.rect.height)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // Запуск выполнения алгоритма
    public void PlayAlgorithm()
    {
        if (!isPlaying && algorithmSteps.Count > 0)
        {
            isPlaying = true;
            StartCoroutine(ExecuteAlgorithm());
        }
    }

    // Базовый метод выполнения алгоритма
    protected virtual IEnumerator ExecuteAlgorithm()
    {
        yield break;
    }

    //Перемещение игрока к цели
    protected IEnumerator MovePlayer(Vector3 targetPosition)
    {
        while (Vector3.Distance(playerTransform.position, targetPosition) > 0.01f)
        {
            if (!isPlaying || isPathBlocked)
                yield break;

            playerTransform.position = Vector3.MoveTowards(
                playerTransform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }
        playerTransform.position = targetPosition;
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

    }

    // Перемещение к чекпоинту точке
    protected virtual IEnumerator MoveToCheckPoint(Transform checkPoint)
    {
        yield return StartCoroutine(MovePlayer(checkPoint.position));
        currentCheckPoint = checkPoint;
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

        for (int i = 0; i < itemsToCollect.Count; i++)
        {
            if (itemsToCollect[i] != null)
            {
                itemsToCollect[i].SetActive(itemActiveStates[i]);
                itemsToCollect[i].transform.position = itemOriginalPositions[i];
            }
        }
    }

    // Показ окна ошибки
    protected virtual void ShowErrorDialog(string message)
    {
        if (DialogeWindowError != null)
        {
            DialogeWindowError.SetActive(true);
            InputField errorText = DialogeWindowError.GetComponentInChildren<InputField>();
            if (errorText != null) errorText.text = message;
        }
    }

    // Обработка столкновений с препятствиями
    protected virtual void HandleObstacleCollision(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            isPathBlocked = true;
            StopAlgorithm();
            if (DialogeWindowBadEnd != null) DialogeWindowBadEnd.SetActive(true);
        }
    }

    // Перезагрузка уровня
    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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

    // Возврат в главное меню
    public void BackToMenu()
    {
        SceneManager.LoadScene("Menu");
    }

    // Удаление последнего шага
    public void RemoveLastStep()
    {
        if (!isPlaying && algorithmSteps.Count > 0)
        {
            algorithmSteps.RemoveAt(algorithmSteps.Count - 1);
            UpdateAlgorithmText();
            StartCoroutine(ScrollIfOverflow());
        }
    }

    // Методы для кнопок
    public void AddUpStep() { AddStep("Вверх"); }
    public void AddDownStep() { AddStep("Вниз"); }
    public void AddLeftStep() { AddStep("Влево"); }
    public void AddRightStep() { AddStep("Вправо"); }
    public void AddGet() { AddStep("Взять"); }
}
