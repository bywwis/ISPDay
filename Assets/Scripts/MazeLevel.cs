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
    private Vector2Int gridSize = new Vector2Int(10, 10);
    [SerializeField] private float cellSize = 1f;
    [SerializeField] [Range(10, 50)] private int obstacleDensity = 30;
    [SerializeField] private GameObject playerPrefab;

    [Header("Anchor Settings")]
    [SerializeField] private Transform locationObject; // Перетащите сюда LocationObject со сцены

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
        Physics2D.queriesStartInColliders = true;
        Physics2D.queriesHitTriggers = true;
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

    private void SetPlayerStartPosition()
    {
        GameObject newPlayer = Instantiate(
            playerPrefab, 
            checkPoints[0].position, 
            Quaternion.identity, 
            locationObject);
            
        player = newPlayer.transform;
        currentCheckPoint = checkPoints[0];
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

        if (locationObject == null)
        {
            locationObject = GameObject.Find("LocationObject").transform;
            if (locationObject == null)
            {
                Debug.LogError("LocationObject не найден на сцене!");
                return;
            }
        }

        if (randomIndex == 0)
        {
            gridSize = new Vector2Int(6, 7);
        }
        else
        {
            gridSize = new Vector2Int(5, 5);
        }
        
        // Генерация остальных элементов
        GenerateCheckpoints();
        GenerateMaze();
        
        // Установка начальной позиции игрока
        SetPlayerStartPosition(); 

        currentLocation.transform.SetAsFirstSibling(); 
    }

    private void GenerateCheckpoints()
    {
        checkPoints.Clear();
        
        // Генерация относительно locationObject
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                Vector3 position = locationObject.position + 
                                 new Vector3(x * cellSize, y * cellSize, 0);
                
                var checkpoint = Instantiate(
                    checkpointPrefab, 
                    position, 
                    Quaternion.identity, 
                    locationObject); // Делаем дочерним
                
                checkpoint.name($"CheckPoint({x})({y})");
                checkPoints.Add(checkpoint.transform);
            }
        }
    }

    private void GenerateMaze()
    {
        // Очистка старых препятствий
        var oldObstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (var obs in oldObstacles) Destroy(obs);
        
        if (obstaclePrefab == null)
        {
            Debug.LogError("Obstacle prefab is not assigned!");
            return;
        }

        // Установка конечной точки (противоположный угол от старта)
        endPoint = new Vector2Int(gridSize.x - 2, gridSize.y - 2);

        // 1. Создаем границы лабиринта
        CreateBorderWalls();

        // 2. Генерируем случайные комнаты
        GenerateRandomRooms();

        // 3. Создаем основной путь от старта до финиша
        GenerateMainPath();

        // 4. Добавляем случайные ответвления
        GenerateRandomBranches();

        // 5. Убедимся, что путь существует
        EnsurePathExists();
    }

    private void CreateBorderWalls()
    {
        // Создаем стены по периметру
        for (int x = 0; x < gridSize.x; x++)
        {
            CreateObstacle(x, 0); // Нижняя стена
            CreateObstacle(x, gridSize.y - 1); // Верхняя стена
        }
        
        for (int y = 1; y < gridSize.y - 1; y++)
        {
            CreateObstacle(0, y); // Левая стена
            CreateObstacle(gridSize.x - 1, y); // Правая стена
        }
    }

    private void GenerateRandomRooms()
    {
        int roomCount = Random.Range(3, 6); // Количество комнат
        
        for (int i = 0; i < roomCount; i++)
        {
            int roomWidth = Random.Range(3, 6);
            int roomHeight = Random.Range(3, 6);
            
            int startX = Random.Range(1, gridSize.x - roomWidth - 1);
            int startY = Random.Range(1, gridSize.y - roomHeight - 1);
            
            // Создаем комнату (очищаем область)
            for (int x = startX; x < startX + roomWidth; x++)
            {
                for (int y = startY; y < startY + roomHeight; y++)
                {
                    RemoveObstacleAt(x, y);
                }
            }
            
            // Добавляем стены вокруг комнаты с некоторой вероятностью
            if (Random.value > 0.5f)
            {
                for (int x = startX - 1; x <= startX + roomWidth; x++)
                {
                    if (Random.value > 0.3f) CreateObstacle(x, startY - 1);
                    if (Random.value > 0.3f) CreateObstacle(x, startY + roomHeight);
                }
                
                for (int y = startY; y < startY + roomHeight; y++)
                {
                    if (Random.value > 0.3f) CreateObstacle(startX - 1, y);
                    if (Random.value > 0.3f) CreateObstacle(startX + roomWidth, y);
                }
            }
        }
    }

    private void GenerateMainPath()
    {
        // Алгоритм "пьяницы" для создания извилистого пути
        Vector2Int currentPos = startPoint;
        int steps = 0;
        int maxSteps = gridSize.x * gridSize.y * 2;
        
        while (currentPos != endPoint && steps < maxSteps)
        {
            // Очищаем текущую позицию
            RemoveObstacleAt(currentPos.x, currentPos.y);
            
            // Определяем направление к цели
            Vector2Int direction;
            if (Random.value > 0.6f)
            {
                // Случайное направление
                direction = RandomDirection();
            }
            else
            {
                // Направление к цели
                direction = new Vector2Int(
                    endPoint.x > currentPos.x ? 1 : endPoint.x < currentPos.x ? -1 : 0,
                    endPoint.y > currentPos.y ? 1 : endPoint.y < currentPos.y ? -1 : 0);
                
                if (direction.x != 0 && direction.y != 0 && Random.value > 0.5f)
                {
                    direction = Random.value > 0.5f ? new Vector2Int(direction.x, 0) : new Vector2Int(0, direction.y);
                }
            }
            
            // Проверяем новую позицию
            Vector2Int newPos = currentPos + direction;
            
            if (IsInBounds(newPos))
            {
                currentPos = newPos;
                
                // С некоторой вероятностью создаем "комнату" на пути
                if (Random.value > 0.8f)
                {
                    CreatePathRoom(currentPos);
                }
            }
            
            steps++;
        }
    }

    private Vector2Int RandomDirection()
    {
        int val = Random.Range(0, 4);
        switch (val)
        {
            case 0: return Vector2Int.up;
            case 1: return Vector2Int.right;
            case 2: return Vector2Int.down;
            default: return Vector2Int.left;
        }
    }

    private void CreatePathRoom(Vector2Int center)
    {
        int size = Random.Range(1, 3);
        
        for (int x = center.x - size; x <= center.x + size; x++)
        {
            for (int y = center.y - size; y <= center.y + size; y++)
            {
                if (IsInBounds(new Vector2Int(x, y)))
                {
                    RemoveObstacleAt(x, y);
                }
            }
        }
    }

    private void GenerateRandomBranches()
    {
        // Добавляем случайные ответвления от основного пути
        int branchCount = Random.Range(5, 10);
        
        for (int i = 0; i < branchCount; i++)
        {
            // Выбираем случайную точку на сетке
            Vector2Int start = new Vector2Int(
                Random.Range(1, gridSize.x - 1),
                Random.Range(1, gridSize.y - 1));
            
            if (IsEmpty(start.x, start.y))
            {
                int length = Random.Range(2, 6);
                Vector2Int dir = RandomDirection();
                
                for (int j = 0; j < length; j++)
                {
                    Vector2Int pos = start + dir * j;
                    if (IsInBounds(pos))
                    {
                        RemoveObstacleAt(pos.x, pos.y);
                        
                        // С некоторой вероятностью меняем направление
                        if (Random.value > 0.7f)
                        {
                            dir = RandomDirection();
                        }
                    }
                }
            }
        }
    }

    private void EnsurePathExists()
    {
        // Используем поиск в ширину для проверки достижимости
        if (!IsPathPossible(startPoint, endPoint))
        {
            // Если путь невозможен, очищаем дополнительные клетки
            Debug.Log("Путь не существует, очищаем дополнительные клетки...");
            
            // Создаем прямой путь по диагонали
            Vector2Int current = startPoint;
            while (current != endPoint)
            {
                RemoveObstacleAt(current.x, current.y);
                
                if (current.x < endPoint.x) current.x++;
                else if (current.x > endPoint.x) current.x--;
                
                if (current.y < endPoint.y) current.y++;
                else if (current.y > endPoint.y) current.y--;
            }
            RemoveObstacleAt(endPoint.x, endPoint.y);
        }
    }

    private bool IsPathPossible(Vector2Int start, Vector2Int end)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        
        queue.Enqueue(start);
        visited.Add(start);
        
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            
            if (current == end)
            {
                return true;
            }
            
            foreach (Vector2Int dir in new Vector2Int[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
            {
                Vector2Int neighbor = current + dir;
                
                if (IsInBounds(neighbor) && !visited.Contains(neighbor) && IsEmpty(neighbor.x, neighbor.y))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
        
        return false;
    }

    private bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridSize.x && pos.y >= 0 && pos.y < gridSize.y;
    }

    private bool IsEmpty(int x, int y)
    {
        Vector3 position = locationObject.position + new Vector3(x * cellSize, y * cellSize, 0);
        Collider2D[] colliders = Physics2D.OverlapPointAll(position);
        return colliders.All(c => !c.CompareTag("Obstacle"));
    }

    private void CreateObstacle(int x, int y)
    {
        Vector3 position = locationObject.position + 
                          new Vector3(x * cellSize, y * cellSize, 0);
        
        Instantiate(
            obstaclePrefab, 
            position, 
            Quaternion.identity, 
            locationObject) // Делаем дочерним
            .tag = "Obstacle";
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
        scrollRect = algorithmText.GetComponentInParent<ScrollRect>();
        if (scrollRect == null)
        {
            Debug.LogError("ScrollRect не найден!");
        }
        scrollRectTransform = scrollRect.GetComponent<RectTransform>();
        textRectTransform = algorithmText.textComponent.GetComponent<RectTransform>();

        CycleButton.onClick.AddListener(OnCycleButtonClicked);
        EndButton.onClick.AddListener(OnEndButtonClicked);
        
        NumberButtons.SetActive(false);
        ButtonsAlgoritm.SetActive(true);
        EndButton.gameObject.SetActive(false);
    }

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

    public void ReportCollision(bool isBlocked)
    {
        isPathBlocked = isBlocked;
        if (isBlocked)
        {
            Debug.Log("Путь заблокирован!");
            if (DialogeWindowBadEnd != null)
                DialogeWindowBadEnd.SetActive(true);
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
            player.position = checkPoints[0].position;
            currentCheckPoint = checkPoints[0];
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