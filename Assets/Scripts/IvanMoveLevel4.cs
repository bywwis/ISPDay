using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class IvanMoveLevel4 : MonoBehaviour
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

    private List<int> cycleIterations = new List<int>(); // Список для хранения количества итераций для каждого цикла
    private bool isCycleActive = false; // Флаг для проверки, активен ли цикл
    private int cycleStartIndex = -1; // Индекс начала цикла
    private int cycleEndIndex = -1;   // Индекс конца цикла
    private bool isCycleComplete = false; // Флаг для проверки завершения цикла

    private const int MaxStepsWithoutCycle = 10; // Максимальное количество строк без цикла
    private const int MaxStepsWithCycle = 26;   // Максимальное количество строк с циклом
    private bool hasCycle = false;

    [SerializeField]
    private GameObject DialogeWindowError;

    private List<Vector3> itemOriginalPositions = new List<Vector3>();
    private List<bool> itemActiveStates = new List<bool>(); 

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        if (checkPoints.Count > 0)
        {
            currentCheckPoint = checkPoints[78]; // Начальный чекпоинт
            player.position = currentCheckPoint.position;
        }

        // Находим все объекты с тегом "Item"
        // Находим все объекты с тегом "Item" и "Fish"
        GameObject[] itemObjects = GameObject.FindGameObjectsWithTag("Item");
        GameObject[] fishObjects = GameObject.FindGameObjectsWithTag("fish");

        itemsToCollect = new List<GameObject>();
        itemsToCollect.AddRange(itemObjects);
        itemsToCollect.AddRange(fishObjects);

        foreach (var item in itemsToCollect)
        {
            if (item != null)
            {
                itemOriginalPositions.Add(item.transform.position);
                itemActiveStates.Add(item.activeSelf);
            }
        }

        if (itemsToCollect.Count == 0)
        {
           Debug.LogWarning("Не найдены объекты с тегами 'Item' или 'fish'.");
        }

        // Ищем чекпоинт
        if (checkPoints.Count > 56)
        {
            targetCheckPoint = checkPoints[56];
        }
        else
        {
            Debug.LogError("Чекпоинт 56 отсутствует в списке checkPoints.");
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

        // Проверка завершения уровня
        if (collectedItemsCount == 1 && targetCheckPoint != null)
        {
            if (Vector3.Distance(player.position, targetCheckPoint.position) < 0.1f)
            {
                ShowCompletionDialog();
            }
        }
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("Menu");
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
                hasCycle = true;
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

        int maxSteps;
        
        if (hasCycle)
        {
            maxSteps = MaxStepsWithCycle + 1;
        }
        else
        {
            maxSteps = MaxStepsWithoutCycle;
        }

        // Проверяем, что количество строк не превышено
        int lineCount = algorithmText.text.Split('\n').Length;

        if (lineCount > maxSteps)
        {
            ShowErrorDialog($"Превышено максимальное количество строк ({maxSteps}). Используйте цикл для компактности.");
            return;
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

    private void ShowErrorDialog(string message)
    {
        if (DialogeWindowError != null)
        {
            DialogeWindowError.SetActive(true);
            InputField errorText = DialogeWindowError.GetComponentInChildren<InputField>();
            if (errorText != null)
            {
                errorText.text = message;
            }
        }
    }

    // Проигрываем алгоритм
    public void PlayAlgorithm()
    {
        // Проверяем, есть ли незавершенные циклы
        if (isCycleActive && !isCycleComplete)
        {
            ShowErrorDialog("Алгоритм не может быть запущен, пока цикл не завершен.");
            StopAlgorithm();
            return;
        }

        // Проверяем, что для всех циклов задано количество итераций
        int cycleCount = algorithmSteps.Count(step => step.StartsWith("Для"));
        if (cycleCount > 0 && cycleIterations.Count != cycleCount)
        {
            ShowErrorDialog("Для всех циклов должно быть задано количество итераций.");
            return;
        }

        if (!isPlaying && algorithmSteps.Count > 0)
        {
            isPlaying = true;
            StartCoroutine(ExecuteAlgorithm());
        }
    }

    // Пошагово выполняем алгоритм
    private IEnumerator ExecuteAlgorithm()
    {
        Stack<int> cycleStack = new Stack<int>(); // Стек для хранения индексов начала и конца циклов
        int cycleIndex = 0; // Индекс для отслеживания текущего цикла

        for (int i = 0; i < algorithmSteps.Count; i++)
        {
            if (!isPlaying || isPathBlocked)
            {
                yield break;
            }

            string step = algorithmSteps[i];

            if (step.StartsWith("Для"))
            {
                // Получаем количество итераций из списка
                int iterations = cycleIterations[cycleIndex];
                cycleIndex++;

                // Запоминаем индекс начала цикла
                cycleStack.Push(i);

                // Переходим к шагам внутри цикла
                for (int j = 1; j < iterations; j++)
                {
                    for (int k = i + 1; k < algorithmSteps.Count; k++)
                    {
                        string innerStep = algorithmSteps[k];

                        if (innerStep == ")")
                        {
                            break; // Завершаем выполнение цикла
                        }

                        yield return StartCoroutine(ExecuteStep(innerStep));
                    }
                }

                // Пропускаем шаги внутри цикла, чтобы не выполнять их повторно
                i = cycleStack.Pop(); // Возвращаемся к началу цикла
            }
            else
            {
                yield return StartCoroutine(ExecuteStep(step));
            }
        }

        // Проверка завершения уровня после выполнения всех шагов
        if (collectedItemsCount == 1 && targetCheckPoint != null)
        {
            if (Vector3.Distance(player.position, targetCheckPoint.position) < 0.1f)
            {
                ShowCompletionDialog();
            }
            else
            {
                // Если персонаж не на правильном чекпоинте, показываем BadEnd
                if (DialogeWindowBadEnd != null)
                {
                    DialogeWindowBadEnd.SetActive(true);
                }
            }
        }
        else
        {
            DialogeWindowBadEnd.SetActive(true);
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

            // Если персонаж взял рыбу, показываем BadEnd
            if (hasFish)
            {
                if (DialogeWindowBadEnd != null)
                {
                    DialogeWindowBadEnd.SetActive(true);
                }
                yield break; // Останавливаем выполнение алгоритма
            }
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
        StopAllCoroutines();

        isCycleActive = false; // Сброс активности цикла
        cycleStartIndex = -1; // Сброс индекса начала цикла
        hasCycle = false;

        if (cycleIterations.Count > 0)
        {
            cycleIterations.Clear(); // Очищаем список итераций
        }

        ResetItems();

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
            player.position = checkPoints[78].position;
            currentCheckPoint = checkPoints[78];
        }
    }

    private void ResetItems()
    {
        collectedItemsCount = 0;
        hasFish = false;
        
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

    private void ExecuteGetCommand()
    {
        for (int i = 0; i < itemsToCollect.Count; i++)
        {
            var item = itemsToCollect[i];
            if (item != null && item.activeSelf)
            {
                float distance = Vector3.Distance(player.position, item.transform.position);

                if (distance < 100f)
                {
                    if (item.CompareTag("fish"))
                    {
                        hasFish = true;
                        if (DialogeWindowBadEnd != null)
                        {
                            DialogeWindowBadEnd.SetActive(true);
                        }
                    }

                    item.SetActive(false);
                    collectedItemsCount++;
                    break;
                }
            }
        }
    }
    

    private void ShowCompletionDialog()
    {
        // Проверяем, был ли собран правильный предмет (например, объект с тегом "Item")
        if (collectedItemsCount == 1 && !hasFish && IsAtCorrectCheckpoint())
        {
            // Если собран правильный предмет, не была найдена рыба и персонаж на правильном чекпоинте, показываем диалоговое окно успеха
            if (DialogeWindowGoodEnd != null)
            {
                DialogeWindowGoodEnd.SetActive(true);
                SaveLoadManager.SaveProgress(SceneManager.GetActiveScene().name);
            }
        }
        else
        {
            // Если была найдена рыба, не собран правильный предмет или персонаж не на правильном чекпоинте, показываем диалоговое окно ошибки
            if (DialogeWindowBadEnd != null)
            {
                DialogeWindowBadEnd.SetActive(true);
            }
        }
    }

    private bool IsAtCorrectCheckpoint()
    {
        // Проверяем, находится ли персонаж на правильном чекпоинте
        if (targetCheckPoint != null)
        {
            return Vector3.Distance(player.position, targetCheckPoint.position) < 0.1f;
        }
        return false;
    }

    // Переход на 5 уровень 
    public void LoadNextScene()
    {
        int nextLevelIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextLevelIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextLevelIndex);
        }
        else
        {
            Debug.Log("Все уровни пройдены!");
        }
    }


    // Методы для кнопок
    public void AddUpStep() 
    { 
        AddStep("Вверх");
        if (CycleButton.gameObject.activeSelf)
        {
            EndButton.gameObject.SetActive(false);
        }
        else
        {
            EndButton.gameObject.SetActive(true);
        }  
    }
    public void AddDownStep() 
    { 
        AddStep("Вниз");
        if (CycleButton.gameObject.activeSelf)
        {
            EndButton.gameObject.SetActive(false);
        }
        else
        {
            EndButton.gameObject.SetActive(true);
        }  
    }
    public void AddLeftStep()
    { 
        AddStep("Влево");
        if (CycleButton.gameObject.activeSelf)
        {
            EndButton.gameObject.SetActive(false);
        }
        else
        {
            EndButton.gameObject.SetActive(true);
        } 
    }
    public void AddRightStep() 
    { 
        AddStep("Вправо");
        if (CycleButton.gameObject.activeSelf)
        {
            EndButton.gameObject.SetActive(false);
        }
        else
        {
            EndButton.gameObject.SetActive(true);
        }  
    }
    public void AddGet() 
    { 
        AddStep("Взять"); 
    }
    public void AddSitStep() { AddStep("Сесть"); }
    public void SetIterations1() { SetIterations(1); NextButton.gameObject.SetActive(true);}
    public void SetIterations2() { SetIterations(2); NextButton.gameObject.SetActive(true);}
    public void SetIterations3() { SetIterations(3); NextButton.gameObject.SetActive(true);}
    public void SetIterations4() { SetIterations(4); NextButton.gameObject.SetActive(true);}
    public void SetIterations5() { SetIterations(5); NextButton.gameObject.SetActive(true);}
    public void SetIterations6() { SetIterations(6); NextButton.gameObject.SetActive(true);}
    public void SetIterations7() { SetIterations(7); NextButton.gameObject.SetActive(true);}
    public void SetIterations8() { SetIterations(8); NextButton.gameObject.SetActive(true);}
    public void SetIterations9() { SetIterations(9); NextButton.gameObject.SetActive(true);}

    void OnCycleButtonClicked()
    {
        // Показываем кнопки для выбора количества итераций
        NumberButtons.SetActive(true);
        ButtonsAlgoritm.SetActive(false);
        EndButton.gameObject.SetActive(false);
        NextButton.gameObject.SetActive(false);
        CycleButton.gameObject.SetActive(false);

        AddStep("Для Ивана от 1 ");
    }

    void OnNextButtonClicked()
    {
        // Показываем кнопки для описания алгоритма
        NumberButtons.SetActive(false);
        ButtonsAlgoritm.SetActive(true);
        EndButton.gameObject.SetActive(false);
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
        cycleIterations.Add(iterations); // Добавляем количество итераций в список
        AddStep($"до {iterations} повторять (");
        NumberButtons.SetActive(false);
    }
}