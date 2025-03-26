using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class IvanMoveLevel2 : MonoBehaviour
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

    private Transform ivan; // Ссылка на Ивана
    private Transform paulina; // Ссылка на Паулину
    private Transform currentIvanCheckPoint; // Текущий чекпоинт Ивана
    private Transform currentPaulinaCheckPoint; // Текущий чекпоинт Паулины

    private bool isInsideCondition = false; // Флаг для проверки, находится ли текущий шаг внутри условия
    private string conditionCharacter = ""; // Персонаж, для которого выполняется условие

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


    void Start()
    {
        // Находим всех персонажей с тегом "Player"
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (var p in players)
        {
            if (p.name == "Ivan") // Если это Иван
            {
                ivan = p.transform;
                currentIvanCheckPoint = checkPoints[32]; // Чекпоинт для Ивана
            }
            else if (p.name == "Paulina") // Если это Паулина
            {
                paulina = p.transform;
                currentPaulinaCheckPoint = checkPoints[44]; // Чекпоинт для Паулины
            }
        }

        // Перемещаем персонажей в их начальные чекпоинты
        if (ivan != null && currentIvanCheckPoint != null)
        {
            ivan.position = currentIvanCheckPoint.position;
        }
        if (paulina != null && currentPaulinaCheckPoint != null)
        {
            paulina.position = currentPaulinaCheckPoint.position;
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
        }
    }

    // Обновляем текстовое поле с алгоритмом
    void UpdateAlgorithmText()
    {
        algorithmText.text = ""; // Очищаем текстовое поле
        int stepNumber = 1; // Нумерация шагов начинается с 1
        bool hasCondition = false; // Флаг для проверки, находится ли текущий шаг внутри условия
        for (int i = 0; i < algorithmSteps.Count; i++)
        {
            // Если шаг начинается с "Если", добавляем его с новой строки
            if (algorithmSteps[i].StartsWith("Если"))
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
                hasCondition = true; // Устанавливаем флаг условия

            }
            // Если шаг начинается с "Иван" или "Паулина", добавляем как часть условия
            else if (algorithmSteps[i].StartsWith("Иван") || algorithmSteps[i].StartsWith("Паулина"))
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
                hasCondition = false; // Сбрасываем флаг условия
            }
            // Обработка обычных шагов (не условий)
            else
            {
                // Если шаг находится внутри условия, добавляем отступ
                if (hasCondition)
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
        if (!isPlaying && algorithmSteps.Count > 0)
        {
            isPlaying = true;
            StartCoroutine(ExecuteAlgorithm());
        }
    }

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

            // Проверяем, начинается ли шаг с условия
            if (step.StartsWith("Если"))
            {
                isInsideCondition = true;
                continue; // Пропускаем этот шаг, так как это начало условия
            }

            // Проверяем, заканчивается ли шаг условием
            if (step.EndsWith(")"))
            {
                isInsideCondition = false;
                conditionCharacter = ""; // Сбрасываем персонажа условия
                continue; // Пропускаем этот шаг, так как это конец условия
            }

            // Если шаг находится внутри условия
            if (isInsideCondition)
            {
                // Определяем, для какого персонажа выполняется условие
                if (step.StartsWith("Иван, то (") || step.StartsWith("Иван"))
                {
                    conditionCharacter = "Иван";
                }
                else if (step.StartsWith("Паулина, то (") || step.StartsWith("Паулина"))
                {
                    conditionCharacter = "Паулина";
                }

                // Если условие уже определено, выполняем действия только для выбранного персонажа
                if (!string.IsNullOrEmpty(conditionCharacter))
                {
                    if (direction != Vector3.zero)
                    {
                        Transform nextCheckPoint = null;

                        if (conditionCharacter == "Иван")
                        {
                            nextCheckPoint = FindNextCheckPoint(direction, currentIvanCheckPoint);
                        }
                        else if (conditionCharacter == "Паулина")
                        {
                            nextCheckPoint = FindNextCheckPoint(direction, currentPaulinaCheckPoint);
                        }

                        if (nextCheckPoint != null)
                        {
                            Transform player;

                            if (conditionCharacter == "Иван")
                            {
                                player = ivan;
                            }
                            else
                            {
                                player = paulina;
                            }
                            yield return StartCoroutine(MovePlayer(player, nextCheckPoint.position));

                            // Обновляем текущий чекпоинт для выбранного персонажа
                            if (conditionCharacter == "Иван")
                            {
                                currentIvanCheckPoint = nextCheckPoint;
                            }
                            else
                            {
                                currentPaulinaCheckPoint = nextCheckPoint;
                            }
                        }
                    }
                }
            }
            else
            {
                // Если шаг не находится внутри условия, выполняем действия для обоих персонажей
                if (direction != Vector3.zero)
                {
                    Transform nextIvanCheckPoint = FindNextCheckPoint(direction, currentIvanCheckPoint);
                    Transform nextPaulinaCheckPoint = FindNextCheckPoint(direction, currentPaulinaCheckPoint);

                    if (nextIvanCheckPoint != null && nextPaulinaCheckPoint != null)
                    {
                        // Запускаем корутины для перемещения обоих персонажей одновременно
                        Coroutine ivanCoroutine = StartCoroutine(MovePlayer(ivan, nextIvanCheckPoint.position));
                        Coroutine paulinaCoroutine = StartCoroutine(MovePlayer(paulina, nextPaulinaCheckPoint.position));

                        // Ждем завершения обеих корутин
                        yield return ivanCoroutine;
                        yield return paulinaCoroutine;

                        // Обновляем текущие чекпоинты
                        currentIvanCheckPoint = nextIvanCheckPoint;
                        currentPaulinaCheckPoint = nextPaulinaCheckPoint;
                    }
                }
            }
        }

        // Проверка чекпоинтов после завершения всего алгоритма
        if (currentIvanCheckPoint == checkPoints[110] && currentPaulinaCheckPoint == checkPoints[57])
        {
            // Показываем диалоговое окно для успешного прохождения уровня
            if (DialogeWindowGoodEnd != null)
            {
                ShowCompletionDialog();
            }
        }
        else
        {
            // Показываем диалоговое окно для проигрыша
            if (DialogeWindowBadEnd != null)
            {
                DialogeWindowBadEnd.SetActive(true);
            }
        }

        isPlaying = false;
    }

    // Двигаем персонажа к целевой позиции
    private IEnumerator MovePlayer(Transform player, Vector3 targetPosition)
    {
        while (Vector3.Distance(player.position, targetPosition) > 0.01f)
        {
            if (!isPlaying || isPathBlocked || DialogeWindowBadEnd.activeSelf || DialogeWindowGoodEnd.activeSelf)
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
    private Transform FindNextCheckPoint(Vector3 direction, Transform currentCheckPoint)
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

        movementButtons.SetActive(true);
        nameButtons.SetActive(false);
        endButton.SetActive(false);
        nextButton.SetActive(false);
        ifButton.SetActive(true);

        algorithmText.text = "";
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }

        if (ivan != null && checkPoints.Count > 32)
        {
            currentIvanCheckPoint = checkPoints[32]; // Назначаем чекпоинт
            ivan.position = currentIvanCheckPoint.position;
        }
        if (paulina != null && checkPoints.Count > 44)
        {
            currentPaulinaCheckPoint = checkPoints[44]; // Назначаем чекпоинт
            paulina.position = currentPaulinaCheckPoint.position;
        }
    }

    // Метод для показа диалогового окна о завершении уровня
    private void ShowCompletionDialog()
    {
        if (DialogeWindowGoodEnd != null)
        {
            DialogeWindowGoodEnd.SetActive(true);
            SaveLoadManager.SaveProgress(SceneManager.GetActiveScene().name);
        }
    }

    // Переход на 3 уровень 
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