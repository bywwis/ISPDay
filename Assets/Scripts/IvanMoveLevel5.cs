using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class IvanMoveLevel5 : BaseMovementController
{
    [SerializeField] private GameObject ifButton;
    [SerializeField] private GameObject movementButtons;
    [SerializeField] private GameObject nameButtons;
    [SerializeField] private GameObject endButton;

    private Transform ivan;
    private Transform paulina;
    private Transform currentIvanCheckPoint;
    private Transform currentPaulinaCheckPoint;
    private bool isInsideCondition = false;
    private string conditionCharacter = "";
    private bool isConditionBeingEdited = false;

    [SerializeField] private List<Transform> checkPoints;

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
                currentIvanCheckPoint = checkPoints[55]; // Чекпоинт для Ивана
                ivan.position = currentIvanCheckPoint.position;
            }
            else if (p.name == "Paulina") // Если это Паулина
            {
                paulina = p.transform;
                currentPaulinaCheckPoint = checkPoints[43]; // Чекпоинт для Паулины
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
            if (isConditionBeingEdited
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
        algorithmText.text = "";
        int stepNumber = 1;
        bool hasCondition = false;

        for (int i = 0; i < algorithmSteps.Count; i++)
        {
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
                stepNumber++;
                hasCondition = true;
            }
            else if (algorithmSteps[i].StartsWith("Иван") || algorithmSteps[i].StartsWith("Паулина"))
            {
                algorithmText.text += $"{algorithmSteps[i]}";
            }
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
                hasCondition = false;
            }
            else
            {
                if (hasCondition)
                {
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
                stepNumber++;
            }
        }

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

            if (step.StartsWith("Если"))
            {
                isInsideCondition = true;
                continue;
            }

            if (step.EndsWith(")"))
            {
                isInsideCondition = false;
                conditionCharacter = "";
                continue;
            }

            if (isInsideCondition)
            {
                if (step.StartsWith("Иван, то (") || step.StartsWith("Иван"))
                {
                    conditionCharacter = "Иван";
                }
                else if (step.StartsWith("Паулина, то (") || step.StartsWith("Паулина"))
                {
                    conditionCharacter = "Паулина";
                }

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
                            Transform player = conditionCharacter == "Иван" ? ivan : paulina;
                            yield return StartCoroutine(MovePlayer(player, nextCheckPoint.position));

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
                if (direction != Vector3.zero)
                {
                    Transform nextIvanCheckPoint = FindNextCheckPoint(direction, currentIvanCheckPoint);
                    Transform nextPaulinaCheckPoint = FindNextCheckPoint(direction, currentPaulinaCheckPoint);

                    if (nextIvanCheckPoint != null && nextPaulinaCheckPoint != null)
                    {
                        Coroutine ivanCoroutine = StartCoroutine(MovePlayer(ivan, nextIvanCheckPoint.position));
                        Coroutine paulinaCoroutine = StartCoroutine(MovePlayer(paulina, nextPaulinaCheckPoint.position));

                        yield return ivanCoroutine;
                        yield return paulinaCoroutine;

                        currentIvanCheckPoint = nextIvanCheckPoint;
                        currentPaulinaCheckPoint = nextPaulinaCheckPoint;
                    }
                }
            }
        }

        if (currentIvanCheckPoint == checkPoints[27] && currentPaulinaCheckPoint == checkPoints[35])
        {
            ShowCompletionDialog();
        }
        else
        {
            if (DialogeWindowBadEnd != null)
            {
                DialogeWindowBadEnd.SetActive(true);
            }
        }

        isPlaying = false;
    }

    private IEnumerator MovePlayer(Transform player, Vector3 targetPosition)
    {
        while (Vector3.Distance(player.position, targetPosition) > 0.01f)
        {
            if (!isPlaying || isPathBlocked || (DialogeWindowBadEnd != null && DialogeWindowBadEnd.activeSelf))
            {
                yield break;
            }

            player.position = Vector3.MoveTowards(player.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        player.position = targetPosition;
    }

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
    private void OnTriggerEnter2D(Collider2D collision) => HandleObstacleCollision(collision);

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

        isConditionBeingEdited = false;

        if (ivan != null && checkPoints.Count > 43)
        {
            currentIvanCheckPoint = checkPoints[43]; // Назначаем чекпоинт
            ivan.position = currentIvanCheckPoint.position;
        }
        if (paulina != null && checkPoints.Count > 55)
        {
            currentPaulinaCheckPoint = checkPoints[55]; // Назначаем чекпоинт
            paulina.position = currentPaulinaCheckPoint.position;
        }
    }

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

        isConditionBeingEdited = true;

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
        OnNextButtonClick();
    }

    // Метод для обработки нажатия на кнопку "Паулина"
    public void OnPaulinaButtonClick()
    {
        // Добавляем текст "Паулина, то ( " в поле алгоритма
        AddStep("Паулина, то ( ");

        // Скрываем кнопки для выбора имени
        nameButtons.SetActive(false);
        OnNextButtonClick();
    }

    public void OnNextButtonClick()
    {
        // Показываем кнопки для движения (они же для описания алгоритма)
        movementButtons.SetActive(true);
        isConditionBeingEdited = true;
    }

    // Метод для обработки нажатия на кнопку "Закончить"
    public void OnEndButtonClick()
    {
        // Возвращаем всё в изначальное положение
        movementButtons.SetActive(true);
        nameButtons.SetActive(false);
        endButton.SetActive(false);
        ifButton.SetActive(true);

        isConditionBeingEdited = false;

        // Добавляем закрывающую скобку и знак ";" в поле алгоритма
        AddStep(")");
    }
}