﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class IvanMoveLevel3 : MonoBehaviour
{
    [SerializeField]
    private InputField algorithmText; // Текстовое поле для отображения алгоритма

    [SerializeField]
    private float moveSpeed = 2f; // Скорость движения персонажа

    [SerializeField]
    private LayerMask obstacleLayer; // Слой для объектов, которые блокируют движение

    private List<string> algorithmSteps = new List<string>(); // Список шагов алгоритма
    private bool isPlaying = false; // Флаг для проверки, проигрывается ли алгоритм

    private bool hasFish = false; // Флаг для проверки, был ли найден объект с тегом "fish"

    private Transform player; // Ссылка на персонажа
    private Transform currentCheckPoint; // Текущий чекпоинт

    [SerializeField]
    private List<Transform> checkPoints; // Список всех чекпоинтов

    private ScrollRect scrollRect;
    private RectTransform scrollRectTransform;
    private RectTransform textRectTransform;

    [SerializeField]
    private GameObject DialogeWindowStory; // Диалоговое окно для истории

    [SerializeField]
    private GameObject DialogeWindowStory2; // Второе диалоговое окно для истории

    [SerializeField]
    private Button NextPageButton; // Кнопка для перехода на следующую страницу

    [SerializeField]
    private GameObject DialogeWindowGoodEnd; // Диалоговое окно для прохождения

    [SerializeField]
    private GameObject DialogeWindowBadEnd; // Диалоговое окно для проигрыша

    private bool isPathBlocked = false; // Флаг для проверки, заблокирован ли путь

    [SerializeField]
    private List<GameObject> itemsToCollect; // Список предметов для сбора
    private int collectedItemsCount = 0; // Счетчик собранных предметов

    private Transform targetCheckPoint; // Чекпоинт (2, 7)

    [SerializeField]
    private Button CycleButton; // Кнопка для начала цикла

    [SerializeField]
    private GameObject NumberButtons; // Группа кнопок для выбора количества итераций

    [SerializeField]
    private Button NextButton; // Кнопка для перехода к описанию алгоритма

    [SerializeField]
    private GameObject ButtonsAlgoritm; // Группа кнопок для описания алгоритма

    [SerializeField]
    private Button EndButton; // Кнопка для завершения цикла

    private int selectedIterations = 1; // Выбранное количество итераций
    private bool isCycleActive = false; // Флаг для проверки, активен ли цикл
    private int cycleStartIndex = -1; // Индекс начала цикла
    private int cycleEndIndex = -1;   // Индекс конца цикла
    private bool isCycleComplete = false; // Флаг для проверки завершения цикла

    void Start()
    {
        if (DialogeWindowStory2 != null)
        {
            DialogeWindowStory2.SetActive(true);
        }

        if (DialogeWindowStory != null)
        {
            DialogeWindowStory.SetActive(true);
        }

        player = GameObject.FindGameObjectWithTag("Player").transform;
        if (checkPoints.Count > 0)
        {
            currentCheckPoint = checkPoints[9]; // Начальный чекпоинт
            player.position = currentCheckPoint.position;
        }

        // Находим все объекты с тегом "Item"
        // Находим все объекты с тегом "Item" и "Fish"
        GameObject[] itemObjects = GameObject.FindGameObjectsWithTag("Item");
        GameObject[] fishObjects = GameObject.FindGameObjectsWithTag("fish");

        itemsToCollect = new List<GameObject>();
        itemsToCollect.AddRange(itemObjects);
        itemsToCollect.AddRange(fishObjects);

        if (itemsToCollect.Count == 0)
        {
            Debug.LogWarning("Не найдены объекты с тегами 'Item' или 'fish'.");
        }

        // Ищем чекпоинт с координатами (1217.60, 913.00, -0.33)
        Vector3 targetPosition = new Vector3(1217.60f, 913.00f, -0.33f);
        targetCheckPoint = FindCheckPointByCoordinates(targetPosition);

        if (targetCheckPoint == null)
        {
            Debug.LogError("Чекпоинт с координатами (1217.60, 913.00, -0.33) не найден.");
        }

        scrollRect = algorithmText.GetComponentInParent<ScrollRect>();
        if (scrollRect == null)
        {
            Debug.LogError("ScrollRect не найден на InputField или его родитель!");
        }

        scrollRectTransform = scrollRect.GetComponent<RectTransform>();
     
        textRectTransform = algorithmText.textComponent.GetComponent<RectTransform>();

        CycleButton.onClick.AddListener(OnCycleButtonClicked);
        NextButton.onClick.AddListener(OnNextButtonClicked);
        EndButton.onClick.AddListener(OnEndButtonClicked);

        // Скрываем группы кнопок при старте
        NumberButtons.SetActive(false);
        ButtonsAlgoritm.SetActive(true);
        EndButton.gameObject.SetActive(false);
        NextButton.gameObject.SetActive(false);

        UpdateAlgorithmText();
    }

    void Update()
    {
        if (isPlaying && algorithmSteps.Count > 0)
        {
            PlayAlgorithm();
        }

        // Проверяем, собраны ли 3 блюда и достигнут ли целевой чекпоинт
        if (collectedItemsCount >= 3 && targetCheckPoint != null)
        {
            if (Vector3.Distance(player.position, targetCheckPoint.position) < 0.01f)
            {
                ShowCompletionDialog();
            }
        }
    }

    // Находим чекпоинт по координатам (x, y, z)
    private Transform FindCheckPointByCoordinates(Vector3 targetPosition)
    {
        foreach (var checkPoint in checkPoints)
        {

            if (Vector3.Distance(checkPoint.position, targetPosition) < 0.1f)
            {
                return checkPoint;
            }
        }
        Debug.Log("Чекпоинт с указанными координатами не найден.");
        return null;
    }

    // Добавляем шаг в алгоритм
    public void AddStep(string step)
    {
        if (!isPlaying)
        {
            algorithmSteps.Add(step);
            UpdateAlgorithmText();

            // Если шаг начинается с "Для", запоминаем индекс начала цикла
            if (step.StartsWith("Для"))
            {
                cycleStartIndex = algorithmSteps.Count - 1;
            }
            // Если шаг — закрывающая скобка ")", запоминаем индекс конца цикла
            else if (step == ")")
            {
                cycleEndIndex = algorithmSteps.Count - 1;
            }
        }
    }

    // Обновляем текстовое поле с алгоритмом
    void UpdateAlgorithmText()
    {
        algorithmText.text = ""; // Очищаем текстовое поле
        int stepNumber = 1; // Нумерация шагов начинается с 1
        for (int i = 0; i < algorithmSteps.Count; i++)
        {
            // Если шаг начинается с "Для", добавляем его с новой строки
            if (algorithmSteps[i].StartsWith("Для"))
            {
                if (stepNumber == 1)
                {
                    algorithmText.text += $"{stepNumber}   {algorithmSteps[i]}";
                }
                else if (stepNumber >= 10)
                {
                    algorithmText.text += $"\n{stepNumber}  {algorithmSteps[i]}";
                }
                else
                {
                    algorithmText.text += $"\n{stepNumber}   {algorithmSteps[i]}";
                }
                stepNumber++; // Увеличиваем номер шага
                isCycleActive = true; // Устанавливаем флаг цикла
                isCycleComplete = false; // Цикл начался, но еще не завершен
            }
            // Если шаг начинается с "до", добавляем как часть условия
            else if (algorithmSteps[i].StartsWith("до"))
            {
                algorithmText.text += $"{algorithmSteps[i]}";
            }
            // Если шаг — закрывающая скобка ")", добавляем её с новой строки
            else if (algorithmSteps[i] == ")")
            {
                if (stepNumber < 10)
                {
                    algorithmText.text += $"\n{stepNumber}   );";
                }
                else
                {
                    algorithmText.text += $"\n{stepNumber}  );";
                }
                stepNumber++;
                isCycleActive = false; // Сбрасываем флаг условия
                isCycleComplete = true; // Цикл завершен
            }
            // Обработка обычных шагов (не условий)
            else
            {
                // Если шаг находится внутри условия, добавляем отступ
                if (isCycleActive)
                {
                    // Отступ для вложенных шагов
                    if (stepNumber < 10)
                    {
                        algorithmText.text += $"\n{stepNumber}     {algorithmSteps[i]};";
                    }
                    else
                    {
                        algorithmText.text += $"\n{stepNumber}    {algorithmSteps[i]};";
                    }
                }
                else
                {
                    // Без отступа
                    if (stepNumber == 1)
                    {
                        algorithmText.text += $"{stepNumber}   {algorithmSteps[i]};";
                    }
                    else if (stepNumber >= 10)
                    {
                        algorithmText.text += $"\n{stepNumber}  {algorithmSteps[i]};";
                    }
                    else
                    {
                        algorithmText.text += $"\n{stepNumber}   {algorithmSteps[i]};";
                    }
                }
                stepNumber++; // Увеличиваем номер шага
            }
        }

        // Прокрутка текстового поля, если текст не помещается
        StartCoroutine(ScrollIfOverflow());
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
        if (!isPlaying && algorithmSteps.Count > 0 && isCycleComplete)
        {
            isPlaying = true;
            StartCoroutine(ExecuteAlgorithm());
        }
        else
        {
            Debug.Log("Алгоритм не может быть запущен, пока цикл не завершен.");
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

            // Если шаг начинается с "Для", начинаем цикл
            if (step.StartsWith("Для"))
            {
                // Получаем количество итераций из шага
                int iterations = selectedIterations;

                // Выполняем шаги внутри цикла
                for (int j = 0; j < iterations; j++)
                {
                    // Переходим к шагам внутри цикла
                    for (int k = cycleStartIndex + 1; k < cycleEndIndex; k++)
                    {
                        string innerStep = algorithmSteps[k];
                        yield return StartCoroutine(ExecuteStep(innerStep));
                    }
                }

                // Пропускаем шаги внутри цикла, чтобы не выполнять их повторно
                i = cycleEndIndex;
            }
            else
            {
                yield return StartCoroutine(ExecuteStep(step));
            }
        }

        // После завершения всех шагов проверяем, нужно ли показать диалоговое окно
        if (collectedItemsCount >= itemsToCollect.Count)
        {
            ShowCompletionDialog();
        }
        isPlaying = false;
    }

    private IEnumerator ExecuteStep(string step)
    {
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
            if (DialogeWindowBadEnd != null)
            {
                DialogeWindowBadEnd.SetActive(true);
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

        NumberButtons.SetActive(false);
        ButtonsAlgoritm.SetActive(true);
        EndButton.gameObject.SetActive(false);
        CycleButton.gameObject.SetActive(true);
        NextButton.gameObject.SetActive(false);

        algorithmText.text = "";
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }

        if (checkPoints.Count > 0)
        {
            player.position = checkPoints[9].position;
            currentCheckPoint = checkPoints[9];
        }
    }

    private void ExecuteGetCommand()
    {

      // Создаем временный список для объектов, которые нужно уничтожить
        List<GameObject> itemsToDestroy = new List<GameObject>();

        // Проверяем все объекты в списке
        foreach (var item in itemsToCollect)
        {
            if (item != null)
            {
                // Рассчитываем расстояние между игроком и объектом
                float distance = Vector3.Distance(player.position, item.transform.position);

                // Если объект находится достаточно близко
                if (distance < 125f) // Используем ваше значение расстояния
                {
                    // Если объект — рыба, устанавливаем флаг
                    if (item.CompareTag("fish"))
                    {
                        hasFish = true; // Устанавливаем флаг, что найден объект "fish"
                    }

                    // Добавляем объект в список для уничтожения
                    itemsToDestroy.Add(item);
                    collectedItemsCount++;
                }
            }
        }

        // Уничтожаем объекты и удаляем их из списка после завершения цикла
        foreach (var item in itemsToDestroy)
        {
            if (item != null)
            {
                Destroy(item); // Уничтожаем объект
                itemsToCollect.Remove(item); // Удаляем объект из списка
            }
        }
    }

    private void ShowCompletionDialog()
    {
        // Проверяем, были ли собраны правильные предметы
        if (!hasFish)
        {
            // Если рыба не была найдена, показываем диалоговое окно успеха
            if (DialogeWindowGoodEnd != null)
            {
                DialogeWindowGoodEnd.SetActive(true);
            }
  
        }
        else
        {
            // Если была найдена рыба, показываем диалоговое окно ошибки
            if (DialogeWindowBadEnd != null)
            {
                DialogeWindowBadEnd.SetActive(true);
            }
        }
    }


    // Методы для кнопок
    public void AddUpStep() { AddStep("Вверх"); }
    public void AddDownStep() { AddStep("Вниз"); }
    public void AddLeftStep() { AddStep("Влево"); }
    public void AddRightStep() { AddStep("Вправо"); }
    public void AddGet() { AddStep("Взять"); }
    public void SetIterations1() { SetIterations(1); }
    public void SetIterations2() { SetIterations(2); }
    public void SetIterations3() { SetIterations(3); }
    public void SetIterations4() { SetIterations(4); }
    public void SetIterations5() { SetIterations(5); }
    public void SetIterations6() { SetIterations(6); }
    public void SetIterations7() { SetIterations(7); }
    public void SetIterations8() { SetIterations(8); }
    public void SetIterations9() { SetIterations(9); }

    void OnCycleButtonClicked()
    {
        // Показываем кнопки для выбора количества итераций
        NumberButtons.SetActive(true);
        ButtonsAlgoritm.SetActive(false);
        EndButton.gameObject.SetActive(false);
        NextButton.gameObject.SetActive(true);
        CycleButton.gameObject.SetActive(false);

        AddStep("Для Ивана от 1 ");
    }

    void OnNextButtonClicked()
    {
        // Показываем кнопки для описания алгоритма
        NumberButtons.SetActive(false);
        ButtonsAlgoritm.SetActive(true);
        EndButton.gameObject.SetActive(true);
        NextButton.gameObject.SetActive(false);

    }

    void OnEndButtonClicked()
    {
        NumberButtons.SetActive(false);
        ButtonsAlgoritm.SetActive(true);
        EndButton.gameObject.SetActive(false);
        CycleButton.gameObject.SetActive(true);

        AddStep(")");
        isCycleComplete = true; // Цикл завершен
    }

    public void SetIterations(int iterations)
    {
        selectedIterations = iterations;
        AddStep($"до {iterations} повторять (");
        NumberButtons.SetActive(false);
    }
}