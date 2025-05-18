using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;


public class ConditionalMovementController : BaseMovementController
{
    [SerializeField] private GameObject ifButton; // Кнопка условия
    [SerializeField] private GameObject movementButtons; // Кнопки для движения
    [SerializeField] private GameObject nameButtons; // Кнопки для выбора имени
    [SerializeField] private GameObject endButton; // Кнопка Закончить Условие

    private Transform ivan; // Ссылка на Ивана
    private Transform paulina; // Ссылка на Паулину
    protected Transform currentIvanCheckPoint; // Текущий чекпоинт Ивана
    protected Transform currentPaulinaCheckPoint; // Текущий чекпоинт Паулины

    private bool isInsideCondition = false; // Флаг для проверки, находится ли текущий шаг внутри условия
    private string conditionCharacter = ""; // Персонаж, для которого выполняется условие
    private bool isConditionActive = false;

    [SerializeField] private List<Transform> checkPoints; // Список всех чекпоинтов

    protected virtual int GetIvanInitialCheckpointIndex() => 55;
    protected virtual int GetPaulinaInitialCheckpointIndex() => 43;
    protected virtual int GetIvanTargetCheckpointIndex() => 86;
    protected virtual int GetPaulinaTargetCheckpointIndex() => 35;

    protected override void Start()
    {
        base.Start();

        // Находим всех персонажей с тегом "Player"
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (var p in players)
        {
            if (p.name == "Ivan") // Если это Иван
            {
                ivan = p.transform;
                currentIvanCheckPoint = checkPoints[GetIvanInitialCheckpointIndex()]; // Чекпоинт для Ивана
                ivan.position = currentIvanCheckPoint.position;
            }
            else if (p.name == "Paulina") // Если это Паулина
            {
                paulina = p.transform;
                currentPaulinaCheckPoint = checkPoints[GetPaulinaInitialCheckpointIndex()]; // Чекпоинт для Паулины
                paulina.position = currentPaulinaCheckPoint.position;
            }
        }

        endButton.SetActive(false);
        nameButtons.SetActive(false);
    }

    // Добавляем шаг в алгоритм
    public override void AddStep(string step)
    {
        // Вызываем базовую реализацию (добавляет шаг и обновляет текст)
        base.AddStep(step);

        // Дополнительная логика для управления кнопками
        if (!isPlaying)
        {
            if (isConditionActive
                && !step.StartsWith("Если")
                && !step.StartsWith("Иван")
                && !step.StartsWith("Паулина"))
            {
                endButton.SetActive(true);
            }
            else
            {
                endButton.SetActive(false);
            }
        }
    }

    // Обновляем текстовое поле с алгоритмом
    protected override void UpdateAlgorithmText()
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

        int lineCount = algorithmText.text.Split('\n').Length;

        if (lineCount > 25)
        {
            ShowErrorDialog($"Превышено максимальное количество строк (25).");
            return;
        }

        // Прокрутка текстового поля, если текст не помещается
        StartCoroutine(ScrollIfOverflow());
    }

    // Проигрываем алгоритм
    public override void PlayAlgorithm()
    {
        base.PlayAlgorithm();

        // Проверяем, есть ли незавершенное условие в алгоритме
        if (HasUnfinishedCondition())
        {
            // Показываем окно ошибки
            if (DialogeWindowError != null)
            {
                DialogeWindowError.SetActive(true);
            }
            return;
        }
    }

    // Проверяет, есть ли незавершенное условие в алгоритме
    private bool HasUnfinishedCondition()
    {
        bool hasOpenCondition = false;

        foreach (string step in algorithmSteps)
        {
            if (step.StartsWith("Если") || step.StartsWith("Иван, то (") || step.StartsWith("Паулина, то ("))
            {
                hasOpenCondition = true;
            }
            else if (step == ")")
            {
                hasOpenCondition = false;
            }
        }

        return hasOpenCondition;
    }

    protected override IEnumerator ExecuteAlgorithm()
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
        if (currentIvanCheckPoint == checkPoints[GetIvanTargetCheckpointIndex()] &&
            currentPaulinaCheckPoint == checkPoints[GetPaulinaTargetCheckpointIndex()])
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
            if (!isPlaying || isPathBlocked || (DialogeWindowBadEnd != null && DialogeWindowBadEnd.activeSelf) || (DialogeWindowGoodEnd != null && DialogeWindowGoodEnd.activeSelf))
            {
                yield break;
            }

            player.position = Vector3.MoveTowards(player.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        player.position = targetPosition;
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

    // Обработчики столкновений
    private void OnTriggerEnter2D(Collider2D collision) { HandleObstacleCollision(collision); }

    // Обработчик события выхода из триггера
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            isPathBlocked = false;
        }
    }

    public override void StopAlgorithm()
    {
        base.StopAlgorithm();

        movementButtons.SetActive(true);
        nameButtons.SetActive(false);
        endButton.SetActive(false);
        ifButton.SetActive(true);

        isConditionActive = false;

        currentIvanCheckPoint = checkPoints[GetIvanInitialCheckpointIndex()]; // Чекпоинт для Ивана
        ivan.position = currentIvanCheckPoint.position;
        currentPaulinaCheckPoint = checkPoints[GetPaulinaInitialCheckpointIndex()]; // Чекпоинт для Паулины
        paulina.position = currentPaulinaCheckPoint.position;
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

    // Метод для обработки нажатия на кнопку "Условие"
    public void OnConditionButtonClick()
    {
        // Показываем кнопки для выбора имени и кнопку "Далее"
        movementButtons.SetActive(false);
        nameButtons.SetActive(true);
        endButton.SetActive(false);
        ifButton.SetActive(false);

        isConditionActive = true;

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
        OnNextClick();
    }

    // Метод для обработки нажатия на кнопку "Паулина"
    public void OnPaulinaButtonClick()
    {
        // Добавляем текст "Паулина, то ( " в поле алгоритма
        AddStep("Паулина, то ( ");

        // Скрываем кнопки для выбора имени
        nameButtons.SetActive(false);
        OnNextClick();
    }

    public void OnNextClick()
    {
        // Показываем кнопки для движения (они же для описания алгоритма)
        movementButtons.SetActive(true);
        isConditionActive = true;
    }

    // Метод для обработки нажатия на кнопку "Закончить"
    public void OnEndButtonClick()
    {
        // Возвращаем всё в изначальное положение
        movementButtons.SetActive(true);
        nameButtons.SetActive(false);
        endButton.SetActive(false);
        ifButton.SetActive(true);

        isConditionActive = false;

        // Добавляем закрывающую скобку и знак ";" в поле алгоритма
        AddStep(")");
    }

    public override void RemoveLastStep()
    {
        if (!isPlaying && algorithmSteps.Count > 0)
        {
            string lastStep = algorithmSteps[^1];
            bool wasConditionClosed = lastStep == ")";

            // Удаляем последний шаг
            algorithmSteps.RemoveAt(algorithmSteps.Count - 1);

            // Если удалили закрывающую скобку - разрешаем редактирование
            if (wasConditionClosed)
            {
                // Ищем начало условия
                for (int i = algorithmSteps.Count - 1; i >= 0; i--)
                {
                    if (algorithmSteps[i].StartsWith("Если"))
                    {
                        // Активируем режим редактирования условия
                        isInsideCondition = true;
                        conditionCharacter = GetConditionCharacter(algorithmSteps[i]);
                        SetupConditionUI();
                        break;
                    }
                }
            }
            // Обработка удаления выбора персонажа
            else if (lastStep.StartsWith("Иван, то (") || lastStep.StartsWith("Паулина, то ("))
            {
                movementButtons.SetActive(false);
                nameButtons.SetActive(true);
                endButton.SetActive(false);
                ifButton.SetActive(false);
            }
            else if (lastStep.StartsWith("Если"))
            {
                movementButtons.SetActive(true);
                nameButtons.SetActive(false);
                endButton.SetActive(false);
                ifButton.SetActive(true);
            }
            else if (isConditionActive)
            {
                // Если удалили шаг внутри условия
                endButton.gameObject.SetActive(false);
            }

            UpdateAlgorithmText();
            StartCoroutine(ScrollIfOverflow());
        }
    }

    private string GetConditionCharacter(string conditionStep)
    {
        if (conditionStep.Contains("Иван")) return "Иван";
        if (conditionStep.Contains("Паулина")) return "Паулина";
        return "";
    }

    private void SetupConditionUI()
    {
        movementButtons.SetActive(true);
        nameButtons.SetActive(false);
        endButton.SetActive(true);
        ifButton.SetActive(false);
        isConditionActive = true;
    }

}

