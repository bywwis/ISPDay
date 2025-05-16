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
    [SerializeField] [Range(30, 95)] private int obstacleDensity = 80;
    [SerializeField] private GameObject playerPrefab;
    private float checkpointX;
    private float checkpointY;
    [SerializeField] private GameObject endPointPrefab;

    [Header("Anchor Settings")]
    [SerializeField] private Transform locationObject; 

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
    private Transform endPointInstance;
    
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
        
        // Проверяем достижение финиша
        CheckEndPointProximity();
    }

    private void CheckEndPointProximity()
    {
        if (player == null || endPointInstance == null) return;

        // Получаем текущий чекпоинт игрока
        Transform nearestCheckpoint = GetNearestCheckpoint(player.position);
        if (nearestCheckpoint == null) return;

        // Получаем чекпоинт конечной точки
        Transform endCheckpoint = GetNearestCheckpoint(endPointInstance.position);
        if (endCheckpoint == null) return;

        // Сравниваем имена чекпоинтов
        if (nearestCheckpoint.name == endCheckpoint.name)
        {
            ShowCompletionDialog(true);
        }
        
        // Дополнительная проверка расстояния (если нужно)
        float distance = Vector3.Distance(player.position, endPointInstance.position);
        if (distance <= 0.9f * cellSize)
        {
            ShowCompletionDialog(true);
        }
    }

    private Transform GetNearestCheckpoint(Vector3 position)
    {
        Transform nearest = null;
        float minDistance = Mathf.Infinity;
        
        foreach (var checkpoint in checkPoints)
        {
            float dist = Vector3.Distance(position, checkpoint.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = checkpoint;
            }
        }
        
        return nearest;
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
            gridSize = new Vector2Int(6, 8);
            checkpointX = 0.9f;
            checkpointY = 0.75f;
        }
        else
        {
            gridSize = new Vector2Int(5, 6);
            checkpointX = 1.2f;
            checkpointY = cellSize;
        }

        endPoint = new Vector2Int(gridSize.x - 1, gridSize.y - 1);
        Debug.Log($"Конечная точка определена: {endPoint}");
        
        // Генерация остальных элементов
        GenerateCheckpoints();
        GenerateMaze();

        CreateEndPoint();
        Debug.Log("Конечная точка создана");
        
        // Установка начальной позиции игрока
        SetPlayerStartPosition(); 

        currentLocation.transform.SetAsFirstSibling(); 
    }

    private Transform GetCheckpointAt(int x, int y)
    {
        // Находим чекпоинт по имени, которое мы задавали в GenerateCheckpoints()
        string checkpointName = $"CheckPoint({x})({y})";
        return checkPoints.FirstOrDefault(cp => cp.name == checkpointName);
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
                                 new Vector3(x * checkpointX, y * checkpointY, 0);
                
                var checkpoint = Instantiate(
                    checkpointPrefab, 
                    position, 
                    Quaternion.identity, 
                    locationObject); // Делаем дочерним
                
                checkpoint.name = $"CheckPoint({x})({y})";
                checkPoints.Add(checkpoint.transform);
            }
        }
    }

    private void CreateEndPoint()
    {
        Transform checkpoint = GetCheckpointAt(gridSize.x - 1, gridSize.y - 1);
        if (checkpoint == null)
        {
            Debug.Log($"Не найден чекпоинт для позиции {endPoint}");
            return;
        }
        
        // Сохраняем ссылку на созданный экземпляр
        endPointInstance = Instantiate(
            endPointPrefab, 
            checkpoint.position, 
            Quaternion.identity, 
            locationObject
        ).transform;
    }

    // Генерация лабиринта
    private void GenerateMaze()
    {
        int attempts = 0;
        const int maxAttempts = 100;
        
        do {
            // Очистка старых препятствий и конечных точек
            var oldObstacles = GameObject.FindGameObjectsWithTag("Obstacle");
            foreach (var obs in oldObstacles) 
            {
                Destroy(obs);
            }
            
            var oldEndPoints = GameObject.FindGameObjectsWithTag("EndPoint");
            foreach (var ep in oldEndPoints)
            {
                Destroy(ep);
            }

            if (obstaclePrefab == null)
            {
                Debug.LogError("Obstacle prefab не найден!");
                return;
            }

            // 1. Генерируем препятствия на сетке
            GenerateGridObstacles();

            // 2. Добавляем случайные ответвления
            GenerateRandomBranches();

            // 3. Создаем гарантированный проход от старта до финиша
            EnsurePathExists();
            
            attempts++;
            if (attempts >= maxAttempts)
            {
                Debug.LogError("Не удалось сгенерировать лабиринт с проходом после " + maxAttempts + " попыток");
                break;
            }
        } while (!PathExists(startPoint, endPoint)); // Повторяем, пока путь не будет существовать
    }

    private bool IsPassagePosition(Vector2Int pos)
    {
        // Определяем середины каждой границы
        int midX = gridSize.x / 2;
        int midY = gridSize.y / 2;
        
        // Проверяем, является ли позиция одним из проходов
        return (pos.x == 0 && pos.y == midY) ||          // Левая граница
            (pos.x == gridSize.x - 1 && pos.y == midY) || // Правая граница
            (pos.y == 0 && pos.x == midX) ||          // Нижняя граница
            (pos.y == gridSize.y - 1 && pos.x == midX);   // Верхняя граница
    }

    private bool IsBorderWall(Vector3 position)
    {
        // Преобразуем мировые координаты в координаты сетки
        Vector2Int gridPos = WorldToGridPosition(position);
        
        // Проверяем, является ли позиция граничной
        return gridPos.x == 0 || gridPos.x == gridSize.x - 1 || 
            gridPos.y == 0 || gridPos.y == gridSize.y - 1;
    }

    private Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        Vector3 localPos = worldPosition - locationObject.position;
        int x = Mathf.RoundToInt(localPos.x / cellSize);
        int y = Mathf.RoundToInt(localPos.y / cellSize);
        return new Vector2Int(x, y);
    }

    private void GenerateGridObstacles()
    {
        // Список позиций, где не должно быть препятствий
        HashSet<Vector2Int> forbiddenPositions = new HashSet<Vector2Int>
        {
            startPoint, // Стартовая позиция игрока
            endPoint    // Конечная точка
        };

        // Добавляем проходы на границах в запрещенные позиции
        int midX = gridSize.x / 2;
        int midY = gridSize.y / 2;
        forbiddenPositions.Add(new Vector2Int(0, midY));      // Левая граница
        forbiddenPositions.Add(new Vector2Int(gridSize.x - 1, midY)); // Правая граница
        forbiddenPositions.Add(new Vector2Int(midX, 0));       // Нижняя граница
        forbiddenPositions.Add(new Vector2Int(midX, gridSize.y - 1)); // Верхняя граница

        for (int x = startPoint.x - 1; x <= startPoint.x; x++)
        {
            for (int y = startPoint.y - 1; y <= startPoint.y; y++)
            {
                if (x >= 0 && x < gridSize.x && y >= 0 && y < gridSize.y)
                {
                    forbiddenPositions.Add(new Vector2Int(x, y));
                }
            }
        }

        // Добавляем путь от старта до финиша в запрещенные позиции
        Vector2Int current = startPoint;
        while (current != endPoint)
        {
            forbiddenPositions.Add(current);
            
            if (current.x < endPoint.x) current.x++;
            else if (current.x > endPoint.x) current.x--;
            
            if (current.y < endPoint.y) current.y++;
            else if (current.y > endPoint.y) current.y--;
            
            // Добавляем соседние клетки к пути с 50% вероятностью
            if (Random.Range(0, 100) < 50)
            {
                forbiddenPositions.Add(new Vector2Int(current.x, current.y + 1));
                forbiddenPositions.Add(new Vector2Int(current.x, current.y - 1));
            }
        }
        forbiddenPositions.Add(endPoint);

        // Генерируем препятствия на сетке, пропуская границы и запрещенные позиции
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                
                // Пропускаем запрещенные позиции
                if (forbiddenPositions.Contains(pos)) continue;
                
                // Для граничных клеток создаем препятствия всегда
                if (IsBorderPosition(pos))
                {
                    CreateObstacle(x, y);
                }
                else if (Random.Range(0, 100) < obstacleDensity) // Для внутренних клеток - с вероятностью
                {
                    CreateObstacle(x, y);
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
            // Выбираем случайную точку на сетке (не на границе)
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
                    if (IsInBounds(pos) && !IsBorderPosition(pos)) // Не трогаем границы
                    {
                        RemoveObstacleAt(pos.x, pos.y);
                        
                        if (Random.value > 0.7f)
                        {
                            dir = RandomDirection();
                        }
                    }
                }
            }
        }
    }

    private bool IsBorderPosition(Vector2Int pos)
    {
        // Позиция на границе, но не является проходом
        return (pos.x == 0 || pos.x == gridSize.x - 1 || 
                pos.y == 0 || pos.y == gridSize.y - 1) &&
            !IsPassagePosition(pos);
    }

    private void EnsurePathExists()
    {
        // Очищаем путь от старта до финиша, не трогая границы
        Vector2Int current = startPoint;
        while (current != endPoint)
        {
            if (!IsBorderPosition(current)) // Не удаляем граничные препятствия
            {
                RemoveObstacleAt(current.x, current.y);
            }
            
            if (current.x < endPoint.x) current.x++;
            else if (current.x > endPoint.x) current.x--;
            
            if (current.y < endPoint.y) current.y++;
            else if (current.y > endPoint.y) current.y--;
        }
        if (!IsBorderPosition(endPoint))
        {
            RemoveObstacleAt(endPoint.x, endPoint.y);
        }
    }

    private bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridSize.x && pos.y >= 0 && pos.y < gridSize.y;
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

    private bool IsEmpty(int x, int y)
    {
        Transform checkpoint = GetCheckpointAt(x, y);
        if (checkpoint == null) return true;
        
        Collider2D[] colliders = Physics2D.OverlapPointAll(checkpoint.position);
        return colliders.All(c => !c.CompareTag("Obstacle"));
    }

    private bool PathExists(Vector2Int start, Vector2Int end)
    {
        // Массив для отслеживания посещенных клеток
        bool[,] visited = new bool[gridSize.x, gridSize.y];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        
        // Начинаем со стартовой позиции
        queue.Enqueue(start);
        visited[start.x, start.y] = true;
        
        // Возможные направления движения (вверх, вправо, вниз, влево)
        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };
        
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            
            // Если достигли конечной точки, возвращаем true
            if (current == end)
                return true;
                
            // Проверяем все возможные направления
            foreach (var dir in directions)
            {
                Vector2Int next = current + dir;
                
                // Проверяем, что следующая клетка в пределах сетки
                if (next.x >= 0 && next.x < gridSize.x && next.y >= 0 && next.y < gridSize.y)
                {
                    // Проверяем, что клетка не посещена и не содержит препятствие
                    if (!visited[next.x, next.y] && IsEmpty(next.x, next.y))
                    {
                        visited[next.x, next.y] = true;
                        queue.Enqueue(next);
                    }
                }
            }
        }
        
        // Если очередь опустела и конечная точка не достигнута
        return false;
    }

    private void CreateObstacle(int x, int y)
    {
        // Находим соответствующий чекпоинт
        Transform checkpoint = GetCheckpointAt(x, y);
        if (checkpoint == null)
        {
            Debug.LogWarning($"Не найден чекпоинт для позиции ({x},{y})");
            return;
        }
        
        // Создаем препятствие на позиции чекпоинта
        Instantiate(
            obstaclePrefab, 
            checkpoint.position, 
            Quaternion.identity, 
            locationObject)
            .tag = "Obstacle";
    }

    private void RemoveObstacleAt(int x, int y)
    {
        // Находим соответствующий чекпоинт
        Transform checkpoint = GetCheckpointAt(x, y);
        if (checkpoint == null) return;
        
        // Ищем препятствия на позиции чекпоинта
        Collider2D[] colliders = Physics2D.OverlapPointAll(checkpoint.position);
        
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Obstacle"))
            {
                Destroy(collider.gameObject);
            }
        }
    }
    // Конец блока генерации лабиринта

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

    private void ShowCompletionDialog(bool success)
    {
        if (success)
        {
            if (DialogeWindowGoodEnd != null)
            {
                DialogeWindowGoodEnd.SetActive(true);
            }
        }
        else if (isPathBlocked)
        {
            if (DialogeWindowBadEnd != null)
            {
                DialogeWindowBadEnd.SetActive(true);
            }
        }
    }

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
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}