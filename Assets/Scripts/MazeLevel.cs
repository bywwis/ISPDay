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

    [Header("Animation Settings")]
    public Animator Ivan_animator;
    public AudioSource clickSound;

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
    private const int MaxStepsWithCycle = 100;
    private bool hasCycle = false;
    
    private GameObject currentLocation;
    private Vector2Int startPoint = new Vector2Int(1, 1);
    private Vector2Int endPoint;

    private ScrollRect scrollRect;
    private RectTransform scrollRectTransform;
    private RectTransform textRectTransform;

    private bool[,] visited;
    private Vector2Int[] directions = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

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
        else
        {
            ShowCompletionDialog(false);
        }
        
        // Дополнительная проверка расстояния (если нужно)
        float distance = Vector3.Distance(player.position, endPointInstance.position);
        if (distance <= 0.9f * cellSize)
        {
            ShowCompletionDialog(true);
        }
        else
        {
            ShowCompletionDialog(false);
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
        if (player != null)
        {
            Destroy(player.gameObject);
        }

        GameObject newPlayer = Instantiate(
            playerPrefab, 
            checkPoints[0].position, 
            Quaternion.identity, 
            locationObject);

        Animator playerAnimator = newPlayer.GetComponent<Animator>();
        if (playerAnimator == null)
        {
            Debug.LogError("Animator отсутствует.");
        }
        else
        {
            Ivan_animator = playerAnimator; 
        }

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

        if (player != null)
        {
            Destroy(player.gameObject);
        }

        if (endPointInstance != null)
        {
            Destroy(endPointInstance.gameObject);
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
            gridSize = new Vector2Int(9, 8);
            checkpointX = 0.7f;
            checkpointY = 0.75f;
        }
        else
        {
            gridSize = new Vector2Int(9, 8);
            checkpointX = 0.7f;
            checkpointY = 0.75f;
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
        if (endPointInstance != null)
        {
            Destroy(endPointInstance.gameObject);
        }

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
        // Любой размер сетки: 10x10, 8x6 и т.д.
        visited = new bool[gridSize.x, gridSize.y];
        ClearObstacles();
        GenerateCheckpoints();
        GenerateMazeWalls();

        // Округляем стартовую позицию до чётной — чтобы RecursiveBacktrack сработал
        Vector2Int adjustedStart = new Vector2Int(
            Mathf.Clamp(startPoint.x | 1, 0, gridSize.x - 1),
            Mathf.Clamp(startPoint.y | 1, 0, gridSize.y - 1)
        );

        RecursiveBacktrack(adjustedStart);
        CreateEndPoint();
        SetPlayerStartPosition();
    }


    private void RecursiveBacktrack(Vector2Int pos)
    {
        if (!IsInBounds(pos)) return;
        visited[pos.x, pos.y] = true;

        var shuffledDirs = directions.OrderBy(_ => Random.value).ToList();

        foreach (var dir in shuffledDirs)
        {
            Vector2Int next = pos + dir * 2;

            if (IsInBounds(next) && !visited[next.x, next.y])
            {
                Vector2Int between = pos + dir;

                RemoveObstacleAt(between.x, between.y);
                RemoveObstacleAt(next.x, next.y);

                RecursiveBacktrack(next);
            }
        }
    }


    private void ClearObstacles()
    {
        foreach (var obj in GameObject.FindGameObjectsWithTag("Obstacle"))
        {
            Destroy(obj);
        }
    }

    private void GenerateMazeWalls()
    {
        HashSet<Vector2Int> forbiddenPositions = new HashSet<Vector2Int>
        {
            startPoint,
            endPoint
        };

        // Запрещаем области вокруг старта и финиша
        for (int x = startPoint.x - 1; x <= startPoint.x; x++)
        {
            for (int y = startPoint.y - 1; y <= startPoint.y; y++)
            {
                if (IsInBounds(new Vector2Int(x, y)))
                {
                    forbiddenPositions.Add(new Vector2Int(x, y));
                }
            }
        }

        for (int x = endPoint.x - 1; x <= endPoint.x; x++)
        {
            for (int y = endPoint.y - 1; y <= endPoint.y; y++)
            {
                if (IsInBounds(new Vector2Int(x, y)))
                {
                    forbiddenPositions.Add(new Vector2Int(x, y));
                }
            }
        }

        // Ставим стены везде, кроме защищённых клеток
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!forbiddenPositions.Contains(pos))
                {
                    CreateObstacle(x, y);
                }
            }
        }
    }

    private void GenerateRandomBranches()
    {
        // Создаем несколько проходов в случайных местах
        int passages = Random.Range(3, 6);
        for (int i = 0; i < passages; i++)
        {
            Vector2Int pos = new Vector2Int(
                Random.Range(1, gridSize.x - 1),
                Random.Range(1, gridSize.y - 1));
            
            // Создаем проход в случайном направлении
            Vector2Int dir = RandomDirection();
            for (int j = 0; j < Random.Range(2, 4); j++)
            {
                Vector2Int passagePos = pos + dir * j;
                if (IsInBounds(passagePos))
                {
                    RemoveObstacleAt(passagePos.x, passagePos.y);
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
        // Используем волновой алгоритм для поиска пути от старта до финиша
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        queue.Enqueue(startPoint);
        cameFrom[startPoint] = startPoint;

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (current == endPoint)
            {
                // Восстанавливаем путь и удаляем препятствия
                while (current != startPoint)
                {
                    RemoveObstacleAt(current.x, current.y);
                    current = cameFrom[current];
                }
                return;
            }

            foreach (var dir in directions)
            {
                Vector2Int next = current + dir;
                if (IsInBounds(next) && !cameFrom.ContainsKey(next) && !IsBorderWall(next))
                {
                    cameFrom[next] = current;
                    queue.Enqueue(next);
                }
            }
        }

        // Если путь не найден, генерируем лабиринт заново
        GenerateMaze();
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

    private bool IsBorderWall(Vector2Int pos)
    {
        // Проверяем, является ли позиция границей с препятствием
        Transform checkpoint = GetCheckpointAt(pos.x, pos.y);
        if (checkpoint == null) return true;

        Collider2D[] colliders = Physics2D.OverlapPointAll(checkpoint.position);
        return colliders.Any(c => c.CompareTag("Obstacle")) && IsBorderPosition(pos);
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

    // Возврат в главное менюe
    public void BackToMenu()
    {
        clickSound.Play();
        Invoke(nameof(CloseLevel), clickSound.clip.length);
    }

    private void CloseLevel()
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
        int stepNumber = 1;
        Stack<int> cycleStartNumbers = new Stack<int>(); // Для хранения номеров начал циклов

        for (int i = 0; i < algorithmSteps.Count; i++)
        {
            string currentStep = algorithmSteps[i];
            string prefix = stepNumber < 10 ? $"{stepNumber}   " : $"{stepNumber}  ";
            string nestedPrefix = stepNumber < 10 ? $"{stepNumber}     " : $"{stepNumber}    ";

            // Начало цикла ("Для...")
            if (currentStep.StartsWith("Для"))
            {
                algorithmText.text += (stepNumber > 1 ? "\n" : "") + prefix + currentStep;
                cycleStartNumbers.Push(stepNumber); // Запоминаем номер начала цикла
                stepNumber++;
                hasCycle = true;
                isCycleActive = true;
                isCycleComplete = false;
            }
            // Условие цикла ("до...")
            else if (currentStep.StartsWith("до"))
            {
                algorithmText.text += " " + currentStep;
            }
            // Конец цикла (")")
            else if (currentStep == ")")
            {
                int cycleStartNumber = cycleStartNumbers.Pop(); // Получаем номер начала цикла
                string closingPrefix = cycleStartNumber < 10 ? $"{stepNumber}   " : $"{stepNumber}  ";
                algorithmText.text += "\n" + closingPrefix + ");";
                stepNumber++;
                isCycleActive = false;
                isCycleComplete = true;
            }
            // Обычные шаги (внутри или вне цикла)
            else
            {
                if (cycleStartNumbers.Count > 0) // Если внутри цикла
                {
                    algorithmText.text += "\n" + nestedPrefix + currentStep + ";";
                }
                else // Если вне цикла
                {
                    algorithmText.text += (stepNumber > 1 ? "\n" : "") + prefix + currentStep + ";";
                }
                stepNumber++;
            }
        }

        StartCoroutine(ScrollIfOverflow());
    }

    private IEnumerator ScrollIfOverflow()
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
        clickSound.Play();
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

        Ivan_animator.SetBool("Move", false);
        isPlaying = false;
        // Проверяем достижение финиша
        CheckEndPointProximity();
    }
    
    private IEnumerator ExecuteStep(string step)
    {
        Vector3 direction = GetDirectionFromStep(step);

        if (direction != Vector3.zero)
        {
            Transform nextCheckPoint = FindNextCheckPoint(direction);
            if (nextCheckPoint != null)
            {
                Ivan_animator.SetBool("Move", true);
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

    // Перезагрузка уровня
    public void RestartLevel()
    {
        clickSound.Play();
        Invoke(nameof(LevelScenLoad), clickSound.clip.length);
    }

    private void LevelScenLoad()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void StopAlgorithm()
    {
        clickSound.Play();
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

        if (player != null && checkPoints.Count > 0)
        {
            player.position = checkPoints[0].position;
            currentCheckPoint = checkPoints[0];
        }

        Ivan_animator.SetBool("Move", false);
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
        else
        {
            DialogeWindowBadEnd.SetActive(true);
        }
    }

    // Методы для кнопок
     public void AddUpStep() 
    {
        clickSound.Play();
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
        clickSound.Play();
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
        clickSound.Play();
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
        clickSound.Play();
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
    public void AddGet() { clickSound.Play(); AddStep("Взять"); }
    public void SetIterations1() { clickSound.Play(); SetIterations(1);}
    public void SetIterations2() { clickSound.Play(); SetIterations(2);}
    public void SetIterations3() { clickSound.Play(); SetIterations(3);}
    public void SetIterations4() { clickSound.Play(); SetIterations(4);}
    public void SetIterations5() { clickSound.Play(); SetIterations(5);}
    public void SetIterations6() { clickSound.Play(); SetIterations(6);}
    public void SetIterations7() { clickSound.Play(); SetIterations(7);}
    public void SetIterations8() { clickSound.Play(); SetIterations(8);}
    public void SetIterations9() { clickSound.Play(); SetIterations(9);}

    void OnCycleButtonClicked()
    {
        clickSound.Play();
        // Показываем кнопки для выбора количества итераций
        NumberButtons.SetActive(true);
        ButtonsAlgoritm.SetActive(false);
        EndButton.gameObject.SetActive(false);
        CycleButton.gameObject.SetActive(false);

        AddStep("Для Ивана от 1");
    }

    void OnNextButtonClicked()
    {
        clickSound.Play();
        // Показываем кнопки для описания алгоритма
        NumberButtons.SetActive(false);
        ButtonsAlgoritm.SetActive(true);
        EndButton.gameObject.SetActive(false);
    }

    void OnEndButtonClicked()
    {
        clickSound.Play();
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

    public void RemoveLastStep()
    {
        clickSound.Play();

        if (!isPlaying && algorithmSteps.Count > 0)
        {
            string lastStep = algorithmSteps[^1];
            bool wasCycleClosed = lastStep == ")";

            // Удаляем последний шаг
            algorithmSteps.RemoveAt(algorithmSteps.Count - 1);

            if (wasCycleClosed)
            {
                // Если удалили закрывающую скобку цикла
                isCycleComplete = false;
                isCycleActive = true;
                CycleButton.gameObject.SetActive(false);
                ButtonsAlgoritm.SetActive(true);
                EndButton.gameObject.SetActive(true);
            }
            else if (lastStep.StartsWith("Для"))
            {
                // Если удалили начало цикла
                isCycleActive = false;
                CycleButton.gameObject.SetActive(true);
                ButtonsAlgoritm.SetActive(true);
                NumberButtons.SetActive(false);
                EndButton.gameObject.SetActive(false);
            }
            else if (lastStep.StartsWith("до"))
            {
                // Если удалили условие итерации
                NumberButtons.SetActive(true);
                ButtonsAlgoritm.SetActive(false);
                EndButton.gameObject.SetActive(false);
                CycleButton.gameObject.SetActive(false);

                // Удаляем соответствующую итерацию
                if (cycleIterations.Count > 0)
                {
                    cycleIterations.RemoveAt(cycleIterations.Count - 1);
                }
            }
            else if (isCycleActive)
            {
                // Если удалили шаг внутри цикла
                EndButton.gameObject.SetActive(false);
            }

            UpdateAlgorithmText();
            StartCoroutine(ScrollIfOverflow());
        }
    }
}