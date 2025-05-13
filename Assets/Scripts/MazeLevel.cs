using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class MazeLevel : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private InputField algorithmText;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private LayerMask obstacleLayer;
    
    [Header("Level Settings")]
    [SerializeField] private List<GameObject> locationPrefabs;
    [SerializeField] private GameObject checkpointPrefab;
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private Vector2Int gridSize = new Vector2Int(10, 10);
    [SerializeField] private float cellSize = 1f;
    [SerializeField] [Range(10, 50)] private int obstacleDensity = 30;
    
    [Header("UI Windows")]
    [SerializeField] private GameObject DialogeWindowGoodEnd;
    [SerializeField] private GameObject DialogeWindowBadEnd;
    [SerializeField] private GameObject DialogeWindowError;
    
    [Header("Cycle Settings")]
    [SerializeField] private Button CycleButton;
    [SerializeField] private GameObject NumberButtons;
    [SerializeField] private GameObject ButtonsAlgoritm;
    [SerializeField] private Button EndButton;

    private List<string> algorithmSteps = new List<string>();
    private bool isPlaying = false;
    private bool isPathBlocked = false;
    
    private Transform player;
    private Transform currentCheckPoint;
    private Transform targetCheckPoint;
    private List<Transform> checkPoints = new List<Transform>();
    
    private List<int> cycleIterations = new List<int>();
    private bool isCycleActive = false;
    private int cycleStartIndex = -1;
    private int cycleEndIndex = -1;
    private bool isCycleComplete = false;
    
    private const int MaxStepsWithoutCycle = 10;
    private const int MaxStepsWithCycle = 17;
    private bool hasCycle = false;
    
    private GameObject currentLocation;
    private Vector2Int startPoint = new Vector2Int(1, 1);
    private Vector2Int endPoint;

    private ScrollRect scrollRect;
    private RectTransform scrollRectTransform;
    private RectTransform textRectTransform;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        GenerateRandomLevel();
        InitializeUI();
    }

    void Update()
    {
        if (isPlaying && algorithmSteps.Count > 0)
        {
            PlayAlgorithm();
        }
    }

    private Vector2 GetCameraBounds()
    {
        Camera mainCamera = Camera.main;
        float height = 2f * mainCamera.orthographicSize;
        float width = height * mainCamera.aspect;
        return new Vector2(width, height);
    }

    private void GenerateRandomLevel()
    {
        // Очистка предыдущего уровня
        if (currentLocation != null) 
        {
            Destroy(currentLocation);
        }
        
        // Проверка наличия префабов
        if (locationPrefabs == null || locationPrefabs.Count == 0)
        {
            Debug.LogError("Список locationPrefabs пуст или не назначен!");
            return;
        }

        // Выбор случайной локации
        int randomIndex = Random.Range(0, locationPrefabs.Count);
        GameObject selectedPrefab = locationPrefabs[randomIndex];
        
        // Создание экземпляра префаба
        currentLocation = Instantiate(
            selectedPrefab,
            Vector3.zero, // Позиция будет корректироваться ниже
            Quaternion.identity,
            transform // Делаем дочерним объектом
        );

        Debug.Log($"Выбрана локация: {randomIndex} - {selectedPrefab.name}");
        
        // Центрирование локации
        Vector2 cameraBounds = GetCameraBounds();
        currentLocation.transform.localPosition = new Vector3(
            -cameraBounds.x/2 + cellSize, 
            -cameraBounds.y/2 + cellSize, 
            0
        );
        
        // Генерация остальных элементов
        /* GenerateCheckpoints();
        GenerateMaze(); */
        
        // Установка начальной позиции игрока
        /* SetPlayerStartPosition(); */
    }

    private void GenerateCheckpoints()
    {
        checkPoints.Clear();
        Vector3 startPos = currentLocation.transform.position;
        
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                Vector3 position = new Vector3(
                    startPos.x + x * cellSize,
                    startPos.y + y * cellSize,
                    0
                );
                
                var checkpoint = Instantiate(checkpointPrefab, position, Quaternion.identity, currentLocation.transform);
                checkPoints.Add(checkpoint.transform);
            }
        }
    }

    private void GenerateMaze()
    {
        // Clear old obstacles
        var oldObstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (var obs in oldObstacles) Destroy(obs);
        
        // Check obstacle prefab
        if (obstaclePrefab == null)
        {
            Debug.LogError("Obstacle prefab is not assigned!");
            return;
        }

        // Create border walls and random obstacles
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                // Skip start and end points
                if ((x == startPoint.x && y == startPoint.y) || (x == endPoint.x && y == endPoint.y))
                    continue;
                
                // Create border walls
                if (x == 0 || y == 0 || x == gridSize.x - 1 || y == gridSize.y - 1)
                {
                    CreateObstacle(x, y);
                    continue;
                }
                
                // Random obstacles
                if (Random.Range(0, 100) < obstacleDensity)
                {
                    CreateObstacle(x, y);
                }
            }
        }
        
        EnsureBasicPath();
    }

    private void CreateObstacle(int x, int y)
    {
        Vector3 position = new Vector3(x * cellSize, y * cellSize, 0);
        var obstacle = Instantiate(obstaclePrefab, position, Quaternion.identity, currentLocation.transform);
        obstacle.tag = "Obstacle";
    }

    private void EnsureBasicPath()
    {
        // Simple path from start to end (right then up)
        for (int x = startPoint.x; x <= endPoint.x; x++)
        {
            RemoveObstacleAt(x, startPoint.y);
        }
        
        for (int y = startPoint.y; y <= endPoint.y; y++)
        {
            RemoveObstacleAt(endPoint.x, y);
        }
    }

    private void RemoveObstacleAt(int x, int y)
    {
        Vector3 position = new Vector3(x * cellSize, y * cellSize, 0);
        Collider2D[] colliders = Physics2D.OverlapPointAll(position);
        
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Obstacle"))
            {
                Destroy(collider.gameObject);
            }
        }
    }

    private void InitializeUI()
    {
        CycleButton.onClick.AddListener(OnCycleButtonClicked);
        EndButton.onClick.AddListener(OnEndButtonClicked);
        
        NumberButtons.SetActive(false);
        ButtonsAlgoritm.SetActive(true);
        EndButton.gameObject.SetActive(false);
    }

    // ... (остальные методы UI и управления алгоритмом остаются без изменений, как в оригинальном файле)
    // Включая:
    // - AddStep, UpdateAlgorithmText, PlayAlgorithm, ExecuteAlgorithm
    // - MovePlayer, FindNextCheckPoint, GetDirectionFromStep
    // - OnTriggerEnter2D, OnTriggerExit2D
    // - StopAlgorithm, RestartLevel, BackToMenu
    // - Методы для кнопок (AddUpStep, AddDownStep и т.д.)
    // - Методы для циклов (OnCycleButtonClicked, OnEndButtonClicked, SetIterations)


    public void BackToMenu()
    {
        SceneManager.LoadScene("Menu");
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

            // Определяем текущее ограничение в зависимости от наличия цикла
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
                // Проверяем, что список cycleIterations не пуст и индекс в пределах диапазона
                if (cycleIterations.Count == 0 || cycleIndex >= cycleIterations.Count)
                {
                    Debug.LogError("Ошибка: список cycleIterations пуст или индекс выходит за пределы.");
                    yield break;
                }

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

        /* if (Vector3.Distance(player.position, targetCheckPoint.position) < 0.1f)
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
        } */
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
        /* else if (step == "Взять")
        {
            ExecuteGetCommand();
        } */
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
        StopAllCoroutines();

        algorithmSteps.Clear();

        isCycleActive = false; // Сброс активности цикла
        cycleStartIndex = -1; // Сброс индекса начала цикла
        hasCycle = false;

        if (cycleIterations.Count > 0)
        {
            cycleIterations.Clear(); // Очищаем список итераций
        }

        /* ResetItems(); */

        NumberButtons.SetActive(false);
        ButtonsAlgoritm.SetActive(true);
        EndButton.gameObject.SetActive(false);
        CycleButton.gameObject.SetActive(true);

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

    /* private void ResetItems()
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
    } */

    // Подбор объекта
    /* private void ExecuteGetCommand()
    {
        // Получаем масштаб канваса
        float scale = canvas.scaleFactor;

        float pickupDistance = 100f * scale;

        // Позиция игрока
        Vector2 playerPos = RectTransformUtility.WorldToScreenPoint(Camera.main, player.position);

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
                    if (item.CompareTag("fish"))
                    {
                        hasFish = true;
                    }

                    item.SetActive(false);
                    collectedItemsCount++;
                    break;
                }
            }
        }
    } */

    /* private void ShowCompletionDialog()
    {
        // Если рыба была найдена или собрано меньше 3 предметов, показываем BadEnd
        if (collectedItemsCount < 3)
        {
            if (DialogeWindowBadEnd != null)
            {
                DialogeWindowBadEnd.SetActive(true);
            }
        }
        else
        {
            // Если всё в порядке, показываем GoodEnd
            if (DialogeWindowGoodEnd != null)
            {
                DialogeWindowGoodEnd.SetActive(true);
                SaveLoadManager.SaveProgress(SceneManager.GetActiveScene().name);
            }
        }

    } */

    // Переход на 4 уровень 
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
    public void AddGet() { AddStep("Взять"); }
    public void SetIterations1() { SetIterations(1);}
    public void SetIterations2() { SetIterations(2);}
    public void SetIterations3() { SetIterations(3);}
    public void SetIterations4() { SetIterations(4);}
    public void SetIterations5() { SetIterations(5);}
    public void SetIterations6() { SetIterations(6);}
    public void SetIterations7() { SetIterations(7);}
    public void SetIterations8() { SetIterations(8);}
    public void SetIterations9() { SetIterations(9);}

    void OnCycleButtonClicked()
    {
        // Показываем кнопки для выбора количества итераций
        NumberButtons.SetActive(true);
        ButtonsAlgoritm.SetActive(false);
        EndButton.gameObject.SetActive(false);
        CycleButton.gameObject.SetActive(false);

        AddStep("Для Ивана от 1 ");
    }

    void OnNextButtonClicked()
    {
        // Показываем кнопки для описания алгоритма
        NumberButtons.SetActive(false);
        ButtonsAlgoritm.SetActive(true);
        EndButton.gameObject.SetActive(false);
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
        OnNextButtonClicked();
    }

    public void RegenerateLevel()
    {
        StopAlgorithm();
        GenerateRandomLevel();
    }
}