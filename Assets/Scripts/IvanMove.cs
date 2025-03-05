using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IvanMove : MonoBehaviour
{
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


    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform; // Находим персонажа по тегу
        if (checkPoints.Count > 0)
        {
            currentCheckPoint = checkPoints[0]; // Начальный чекпоинт
            player.position = currentCheckPoint.position;
        }

        scrollRect = algorithmText.GetComponentInParent<ScrollRect>(); // Ищем ScrollRect на InputField или выше
        if (scrollRect == null)
        {
            Debug.LogError("ScrollRect не найден на InputField или его родителе!");
        }
        
        scrollRectTransform = scrollRect.GetComponent<RectTransform>();

        // Получаем RectTransform текста внутри InputField
        textRectTransform = algorithmText.textComponent.GetComponent<RectTransform>();

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
        algorithmText.text = "";

        for (int i = 0; i < algorithmSteps.Count; i++)
        {
            if (i < 9)
            {
                algorithmText.text += $"{i + 1}   {algorithmSteps[i]};\n";
            }
            else if (i > 9)
            {
               algorithmText.text += $"{i + 1}  {algorithmSteps[i]};\n"; 
            }
            
        }

        StartCoroutine(ScrollIfOverflow());
    }

    private IEnumerator ScrollIfOverflow()
    {
        // Ждем конца кадра, чтобы UI обновился
        yield return null;

        // Принудительно обновляем Canvas
        Canvas.ForceUpdateCanvases();

        // Получаем высоту текста
        float textHeight = LayoutUtility.GetPreferredHeight(textRectTransform);

        // Получаем высоту видимой области ScrollRect
        float scrollRectHeight = scrollRectTransform.rect.height;

        // Прокручиваем только если высота текста больше высоты видимой области
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
    IEnumerator ExecuteAlgorithm()
    {
        for (int i = 0; i < algorithmSteps.Count; i++)
        {
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
        }

        isPlaying = false;
    }

    // Двигаем персонажа к целевой позиции
    IEnumerator MovePlayer(Vector3 targetPosition)
    {
        while (Vector3.Distance(player.position, targetPosition) > 0.01f)
        {
            player.position = Vector3.MoveTowards(player.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        player.position = targetPosition;
    }

    // Получаем направление из шага алгоритма
    Vector3 GetDirectionFromStep(string step)
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
    Transform FindNextCheckPoint(Vector3 direction)
    {
        Transform nearestCheckPoint = null;
        float nearestDistance = Mathf.Infinity;

        foreach (var checkPoint in checkPoints)
        {
            // Проверяем, что чекпоинт находится в нужном направлении
            Vector3 delta = checkPoint.position - currentCheckPoint.position;
            if (Vector3.Dot(delta.normalized, direction.normalized) > 0.9f) // Угол между направлениями близок к 0
            {
                float distance = Vector3.Distance(currentCheckPoint.position, checkPoint.position);
                if (distance < nearestDistance)
                {
                    // Проверяем, есть ли препятствие на пути
                    if (!IsPathBlocked(currentCheckPoint.position, checkPoint.position))
                    {
                        nearestDistance = distance;
                        nearestCheckPoint = checkPoint;
                    }
                }
            }
        }

        return nearestCheckPoint;
    }

    // Проверяем, есть ли препятствие на пути
    bool IsPathBlocked(Vector3 start, Vector3 end)
    {
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);

        // Используем Raycast для проверки препятствий
        RaycastHit2D hit = Physics2D.Raycast(start, direction, distance, obstacleLayer);

        // Если луч столкнулся с объектом, путь заблокирован
        if (hit.collider != null)
        {
            Debug.Log("Путь заблокирован: " + hit.collider.name);
            return true;
        }

        return false;
    }

    public void StopAlgorithm()
    {
        isPlaying = false;

        algorithmSteps.Clear();

        algorithmText.text = "";

        if (checkPoints.Count > 0)
        {
            player.position = checkPoints[0].position;
            currentCheckPoint = checkPoints[0];
        }
    }

    // Методы для кнопок
    public void AddUpStep() { AddStep("Вверх"); }
    public void AddDownStep() { AddStep("Вниз"); }
    public void AddLeftStep() { AddStep("Влево"); }
    public void AddRightStep() { AddStep("Вправо"); }
}