using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class IvanMove : MonoBehaviour
{
    [SerializeField] 
    private GameObject ifButton; // Кнопка условия
    
    [SerializeField] 
    private GameObject movementButtons; // Кнопки для движения

    [SerializeField] 
    private GameObject nameButtons; // Кнопки для выбора имени

    [SerializeField]
    private GameObject nextButton; // Кнопка Далее Условие

    [SerializeField]
    private GameObject endButton; // Кнопка Закончить Условие

    [SerializeField]
    private InputField algorithmText; // Текстовое поле для отображения алгоритма
    
    [SerializeField]
    private float moveSpeed = 2f; // Скорость движения персонажа
    
    [SerializeField]
    private LayerMask obstacleLayer; // Слой для объектов, которые блокируют движение
    
    private List<string> algorithmSteps = new List<string>(); // Список шагов алгоритма
    private bool isPlaying = false; // Флаг для проверки, проигрывается ли алгоритм

    private Transform player; // Ссылка на персонажа
    private Transform currentCheckPoint; // Текущий чекпоинт
    
    [SerializeField]
    private List<Transform> checkPoints; // Список всех чекпоинтов
    
    private ScrollRect scrollRect;
    private RectTransform scrollRectTransform;
    private RectTransform textRectTransform;
    
    [SerializeField]
    private GameObject DialogeWindow1; // Диалоговое окно для истории

    [SerializeField]
    private GameObject DialogeWindow2; // Диалоговое окно для прохождения

    [SerializeField]
    private GameObject DialogeWindow3; // Диалоговое окно для проигрыша

    private bool isPathBlocked = false; // Флаг для проверки, заблокирован ли путь

    [SerializeField]
    private List<GameObject> itemsToCollect; // Список предметов для сбора
    private int collectedItemsCount = 0; // Счетчик собранных предметов

    void Start()
    {
        if (DialogeWindow2 != null)
        {
            DialogeWindow2.SetActive(false);
        }
    
        player = GameObject.FindGameObjectWithTag("Player").transform;
        if (checkPoints.Count > 0)
        {
            currentCheckPoint = checkPoints[4]; // Начальный чекпоинт
            player.position = currentCheckPoint.position;
        }

        // Находим все объекты с тегом "Item"
        GameObject[] itemObjects = GameObject.FindGameObjectsWithTag("Item");
        if (itemObjects.Length > 0)
        {
            itemsToCollect = new List<GameObject>(itemObjects);
        }
        else
        {
            Debug.LogWarning("Не найдены объекты с тегом 'Item'.");
            itemsToCollect = new List<GameObject>(); // Инициализируем пустой список
        }

        scrollRect = algorithmText.GetComponentInParent<ScrollRect>();
        if (scrollRect == null)
        {
            Debug.LogError("ScrollRect не найден на InputField или его родитель!");
        }
        
        scrollRectTransform = scrollRect.GetComponent<RectTransform>();
        
        textRectTransform = algorithmText.textComponent.GetComponent<RectTransform>();

        endButton.SetActive(false);
        nextButton.SetActive(false);
        nameButtons.SetActive(false);
        
        UpdateAlgorithmText();
    }

    void Update()
    {
        if (isPlaying && algorithmSteps.Count > 0)
        {
            PlayAlgorithm();
        }
    }

    // Добавляем шаг в алгоритм
    public void AddStep(string step)
    {
        if (!isPlaying)
        {
            algorithmSteps.Add(step);
            UpdateAlgorithmText();
        }
    }

    // Обновляем текстовое поле с алгоритмом
    void UpdateAlgorithmText()
    {
        algorithmText.text = ""; // Очищаем текстовое поле
        
        int stepNumber = 1; // Счётчик для нумерации строк

        for (int i = 0; i < algorithmSteps.Count; i++)
        {
            // Если это условие (начинается с "Если"), добавляем номер строки
            if (algorithmSteps[i].StartsWith("Если"))
            {
                algorithmText.text += $"{stepNumber}. {algorithmSteps[i]}";
                stepNumber++; // Увеличиваем счётчик
            }
            // Если это имя (начинается с "Иван" или "Паулина"), добавляем без номера
            else if (algorithmSteps[i].StartsWith("Иван") || algorithmSteps[i].StartsWith("Паулина"))
            {
                algorithmText.text += $"{algorithmSteps[i]}";
            }
            // Если это движение (например, "Вверх"), добавляем с новой строки и номером
            else
            {
                algorithmText.text += $"\n{stepNumber}. {algorithmSteps[i]};";
                stepNumber++; // Увеличиваем счётчик
            }
        }
        
        StartCoroutine(ScrollIfOverflow()); // Прокрутка, если текст выходит за пределы поля
    }

    private IEnumerator ScrollIfOverflow()
    {
        yield return null;
        
        Canvas.ForceUpdateCanvases();
        
        float textHeight = LayoutUtility.GetPreferredHeight(textRectTransform);
        
        float scrollRectHeight = scrollRectTransform.rect.height;
        
        if (textHeight > scrollRectHeight)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // Проигрываем алгоритм
    public void PlayAlgorithm()
    {
        if (!isPlaying && algorithmSteps.Count > 0)
        {
            isPlaying = true;
            StartCoroutine(ExecuteAlgorithm());
        }
    }

    // Пошагово выполняем алгоритм
    private IEnumerator ExecuteAlgorithm()
    {
        for (int i = 0; i < algorithmSteps.Count; i++)
        {
            if (!isPlaying || isPathBlocked)
            {
                yield break;
            }
        
            string step = algorithmSteps[i];
            Vector3 direction = GetDirectionFromStep(step);
            
            if (direction != Vector3.zero)
            {
                Transform nextCheckPoint = FindNextCheckPoint(direction);
                if (nextCheckPoint != null)
                {
                    yield return StartCoroutine(MovePlayer(nextCheckPoint.position));
                    currentCheckPoint = nextCheckPoint;
                }
            }
            else if (step == "Взять")
            {
                ExecuteGetCommand();
            }
        }
        isPlaying = false;
    }

    // Двигаем персонажа к целевой позиции
    private IEnumerator MovePlayer(Vector3 targetPosition)
    {
        while (Vector3.Distance(player.position, targetPosition) > 0.01f)
        {
            if (!isPlaying || isPathBlocked)
            {
                yield break;
            }
            
            player.position = Vector3.MoveTowards(player.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }
        
        player.position = targetPosition;
    }

    // Получаем направление из шага алгоритма
    private Vector3 GetDirectionFromStep(string step)
    {
        switch (step)
        {
            case "Вверх":
                return Vector3.up;
            case "Вниз":
                return Vector3.down;
            case "Влево":
                return Vector3.left;
            case "Вправо":
                return Vector3.right;
            default:
                return Vector3.zero;
        }
    }

    // Находим следующий чекпоинт в заданном направлении
    private Transform FindNextCheckPoint(Vector3 direction)
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

    // Обработчик события входа в триггер
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            isPathBlocked = true;
            Debug.Log("Путь заблокирован: " + collision.gameObject.name);
        
            // Останавливаем выполнение алгоритма
            StopAlgorithm();
            
            // Показываем диалоговое окно
            if (DialogeWindow3 != null)
            {
                DialogeWindow3.SetActive(true);
            }
        }
    }

    // Обработчик события выхода из триггера
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            isPathBlocked = false;
        }
    }

    // Перезапускаем уровень
    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void StopAlgorithm()
    {
        isPlaying = false;
        
        algorithmSteps.Clear();
        
        algorithmText.text = "";
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
        
        if (checkPoints.Count > 0)
        {
            player.position = checkPoints[4].position;
            currentCheckPoint = checkPoints[4];
        }
    }

    private void ExecuteGetCommand()
    {
        // Проверяем, находится ли персонаж рядом с предметом
        foreach (var item in itemsToCollect)
        {
            if (item != null)
            {
                float distance = Vector3.Distance(player.position, item.transform.position);

                if (distance < 200f)
                {
                    Destroy(item);
                    collectedItemsCount++;

                    // Проверяем, собраны ли все предметы
                    if (collectedItemsCount >= 4)
                    {
                        ShowCompletionDialog();
                    }
                    break;
                }
            }
        }
    }

    // Метод для показа диалогового окна о завершении сбора всех предметов
    private void ShowCompletionDialog()
    {
        if (DialogeWindow2 != null)
        {
            DialogeWindow2.SetActive(true);
        }
    }

    // Методы для кнопок
    public void AddUpStep() { AddStep("Вверх"); }
    public void AddDownStep() { AddStep("Вниз"); }
    public void AddLeftStep() { AddStep("Влево"); }
    public void AddRightStep() { AddStep("Вправо"); }
    public void AddSit() { AddStep("Сесть"); }

    // Метод для обработки нажатия на кнопку "Условие"
    public void OnConditionButtonClick()
    {
        // Показываем кнопки для выбора имени и кнопку "Далее"
        movementButtons.SetActive(false);
        nameButtons.SetActive(true);
        endButton.SetActive(false);
        nextButton.SetActive(true);
        ifButton.SetActive(false);

        // Добавляем текст "Если " в поле алгоритма
        AddStep("Если ");
    }

    // Метод для обработки нажатия на кнопку "Иван"
    public void OnIvanButtonClick()
    {
        // Добавляем текст "Иван, то ( " в поле алгоритма
        AddStep("Иван, то ( ");

        // Скрываем кнопки для выбора имени
        nameButtons.SetActive(false);
    }

    // Метод для обработки нажатия на кнопку "Паулина"
    public void OnPaulinaButtonClick()
    {
        // Добавляем текст "Паулина, то ( " в поле алгоритма
        AddStep("Паулина, то ( ");

        // Скрываем кнопки для выбора имени
        nameButtons.SetActive(false);
    }

    // Метод для обработки нажатия на кнопку "Далее"
    public void OnNextButtonClick()
    {
        // Показываем кнопки для движения (они же для описания алгоритма) и кнопку "Закончить"
        movementButtons.SetActive(true);
        nameButtons.SetActive(false);
        endButton.SetActive(true);
        nextButton.SetActive(false);
        ifButton.SetActive(false);

    }

    // Метод для обработки нажатия на кнопку "Закончить"
    public void OnEndButtonClick()
    {
        // Возвращаем всё в изначальное положение
        movementButtons.SetActive(true);
        nameButtons.SetActive(false);
        endButton.SetActive(false);
        nextButton.SetActive(false);
        ifButton.SetActive(true);

        // Добавляем закрывающую скобку и знак ";" в поле алгоритма
        AddStep(")");
    }
}