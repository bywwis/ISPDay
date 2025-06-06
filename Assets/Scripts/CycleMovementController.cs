using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CycleMovementController : BaseMovementController
{
    [SerializeField] protected GameObject cycleButton; // Кнопка для начала цикла
    [SerializeField] protected GameObject numberButtons; // Группа кнопок для выбора количества итераций
    [SerializeField] protected GameObject buttonsAlgoritm; // Группа кнопок для описания алгоритма
    [SerializeField] protected GameObject endButton; // Кнопка для завершения цикла

    protected bool hasWrongItem = false;

    private List<int> cycleIterations = new List<int>(); // Список для хранения количества итераций для каждого цикла
    private bool isCycleActive = false; // Флаг для проверки, активен ли цикл
    private int cycleStartIndex = -1; // Индекс начала цикла
    private int cycleEndIndex = -1;   // Индекс конца цикла
    private bool isCycleComplete = false; // Флаг для проверки завершения цикла
    private bool hasCycle = false;

    protected virtual int GetRequiredItemsCount() => 3;
    protected virtual string GetWrongItemTag() => "WrongItem";
    protected virtual int GetMaxStepsWithoutCycle() => 10;
    protected virtual int GetMaxStepsWithCycle() => 17;

    protected override void Start()
    {
        base.Start();
        InitializeItems();
        SetupCycleButtons();
    }

    protected override void InitializeItems(string itemTag = "Item")
    {
        // Ищем объекты для сбора (обычные и неправильные)
        GameObject[] itemObjects = GameObject.FindGameObjectsWithTag(itemTag);
        GameObject[] wrongObjects = GameObject.FindGameObjectsWithTag(GetWrongItemTag());

        itemsToCollect = new List<GameObject>();
        itemsToCollect.AddRange(itemObjects);
        itemsToCollect.AddRange(wrongObjects);

        foreach (var item in itemsToCollect)
        {
            if (item != null)
            {
                itemOriginalPositions.Add(item.transform.position);
                itemActiveStates.Add(item.activeSelf);
            }
        }
    }

    private void SetupCycleButtons()
    {
        numberButtons.SetActive(false);
        buttonsAlgoritm.SetActive(true);
        endButton.gameObject.SetActive(false);
    }

    // Добавляем шаг в алгоритм
    public override void AddStep(string step)
    {
        base.AddStep(step);

        if (!isPlaying)
        {
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
            if (hasCycle) maxSteps = GetMaxStepsWithCycle() + 1;
            else maxSteps = GetMaxStepsWithoutCycle();


            // Проверяем, что количество строк не превышено
            int lineCount = algorithmText.text.Split('\n').Length;

            if (lineCount > maxSteps)
            {
                ShowErrorDialog($"Превышено максимальное количество строк ({maxSteps}). Используйте цикл для компактности.");
                return;
            }

            if (cycleButton.gameObject.activeSelf) endButton.gameObject.SetActive(false);
            else if (numberButtons.gameObject.activeSelf) endButton.gameObject.SetActive(false);
            else endButton.gameObject.SetActive(true);

        }
    }

    // Обновляем текстовое поле с алгоритмом
    protected override void UpdateAlgorithmText()
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

    // Проигрываем алгоритм
    public override void PlayAlgorithm()
    {
        // Проверяем, есть ли незавершенные циклы
        if (isCycleActive && !isCycleComplete)
        {
            ShowErrorDialog("Алгоритм не может быть запущен, пока цикл не завершен.");
            StopAlgorithm();
            return;
        }

        base.PlayAlgorithm();
    }

    // Пошагово выполняем алгоритм
    protected override IEnumerator ExecuteAlgorithm()
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
        CheckLevelCompletion();
        isPlaying = false;
    }

    protected virtual IEnumerator ExecuteStep(string step)
    {
        Vector3 direction = GetDirectionFromStep(step);

        if (direction != Vector3.zero)
        {
            Transform nextCheckPoint = FindNextCheckPoint(direction, currentCheckPoint);
            if (nextCheckPoint != null)
            {
                Ivan_animator.SetBool("Move", true);
                yield return StartCoroutine(MovePlayer(nextCheckPoint.position));
                currentCheckPoint = nextCheckPoint;
            }
        }
        else if (step == "Взять")
        {
            ExecuteGetCommand();
        }
    }

    // Подбор объекта
    protected override void ExecuteGetCommand()
    {
        // Mасштаб канваса
        float pickupDistance = 100f * canvas.scaleFactor;

        // Позиция игрока
        Vector2 playerPos = RectTransformUtility.WorldToScreenPoint(Camera.main, playerTransform.position);

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
                    if (item.CompareTag(GetWrongItemTag()))
                    {
                        hasWrongItem = true;
                    }
                    GetSound.Play();
                    item.SetActive(false);
                    collectedItemsCount++;
                    break;
                }
            }
        }
    }

    public override void StopAlgorithm()
    {
        base.StopAlgorithm();

        isCycleActive = false; // Сброс активности цикла
        cycleStartIndex = -1; // Сброс индекса начала цикла
        hasCycle = false;

        if (cycleIterations.Count > 0)
        {
            cycleIterations.Clear(); // Очищаем список итераций
        }

        numberButtons.SetActive(false);
        buttonsAlgoritm.SetActive(true);
        endButton.gameObject.SetActive(false);
        cycleButton.gameObject.SetActive(true);

        ResetItems();
    }

    protected override void ResetItems()
    {
        base.ResetItems();
        hasWrongItem = false;
    }

    public void OnCycleButtonClicked()
    {
        clickSound.Play();

        if (!isPlaying)
        {
            isCycleActive = true;
            isCycleComplete = false;

            // Показываем кнопки для выбора количества итераций
            numberButtons.SetActive(true);
            buttonsAlgoritm.SetActive(false);
            cycleButton.gameObject.SetActive(false);
            //EndButton.gameObject.SetActive(false);

            AddStep("Для Ивана от 1");
        }
    }

    public void OnEndButtonClicked()
    {
        clickSound.Play();

        isCycleActive = false;
        isCycleComplete = true; // Цикл завершен

        numberButtons.SetActive(false);
        buttonsAlgoritm.SetActive(true);
        endButton.gameObject.SetActive(false);
        cycleButton.gameObject.SetActive(true);

        AddStep(")");
    }

    public void SetIterations(int iterations)
    {
        cycleIterations.Add(iterations); // Добавляем количество итераций в список
        AddStep($"до {iterations} повторять (");
        numberButtons.SetActive(false);
        buttonsAlgoritm.SetActive(true);
        endButton.gameObject.SetActive(false);
    }

    public void SetIterations1() { clickSound.Play(); SetIterations(1); }
    public void SetIterations2() { clickSound.Play(); SetIterations(2); }
    public void SetIterations3() { clickSound.Play(); SetIterations(3); }
    public void SetIterations4() { clickSound.Play(); SetIterations(4); }
    public void SetIterations5() { clickSound.Play(); SetIterations(5); }
    public void SetIterations6() { clickSound.Play(); SetIterations(6); }
    public void SetIterations7() { clickSound.Play(); SetIterations(7); }
    public void SetIterations8() { clickSound.Play(); SetIterations(8); }
    public void SetIterations9() { clickSound.Play(); SetIterations(9); }

    public override void RemoveLastStep()
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
                cycleButton.gameObject.SetActive(false);
                buttonsAlgoritm.SetActive(true);
                endButton.gameObject.SetActive(true);
            }
            else if (lastStep.StartsWith("Для"))
            {
                // Если удалили начало цикла
                isCycleActive = false;
                cycleButton.gameObject.SetActive(true);
                buttonsAlgoritm.SetActive(true);
                numberButtons.SetActive(false);
                endButton.gameObject.SetActive(false);
            }
            else if (lastStep.StartsWith("до"))
            {
                // Если удалили условие итерации
                numberButtons.SetActive(true);
                buttonsAlgoritm.SetActive(false);
                endButton.gameObject.SetActive(false);
                cycleButton.gameObject.SetActive(false);

                // Удаляем соответствующую итерацию
                if (cycleIterations.Count > 0)
                {
                    cycleIterations.RemoveAt(cycleIterations.Count - 1);
                }
            }
            else if (isCycleActive)
            {
                // Если удалили шаг внутри цикла
                endButton.gameObject.SetActive(false);
            }

            UpdateAlgorithmText();
            StartCoroutine(ScrollIfOverflow());
        }
    }

}
