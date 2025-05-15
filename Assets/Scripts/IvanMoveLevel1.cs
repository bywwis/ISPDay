using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class IvanMoveLevel1 : BaseMovementController
{
    [SerializeField] private List<Transform> checkPoints; // Список всех чекпоинтов

    public Canvas canvas;
    private Transform targetCheckPoint;

    protected override void Start()
    {
        base.Start();
        InitializeCheckpoints();
        InitializeItems();
    }

    private void InitializeCheckpoints()
    {
        if (checkPoints.Count > 0)
        {
            currentCheckPoint = checkPoints[4];
            playerTransform.position = currentCheckPoint.position;
        }

        if (checkPoints.Count > 19) targetCheckPoint = checkPoints[19];

    }

    protected override void UpdateAlgorithmText()
    {
        base.UpdateAlgorithmText(); 

        // Проверка конкретно для первого уровня
        int lineCount = algorithmText.text.Split('\n').Length - 1; // -1 для пустой строки в конце
        if (lineCount > 19)
        {
            ShowErrorDialog($"Превышено максимальное количество строк (19).");
            algorithmText.text = algorithmText.text.Substring(0, algorithmText.text.Length - 1);
        }
    }

    protected override string FormatStepLine(int index)
    {
        string numberPadding; // отступ

        if (index < 9)
        {
            // Если индекс меньше 9 (шаги 1-9)
            numberPadding = "   "; // 3 пробела
        }
        else
        {
            // Если индекс 9 и больше (шаги 10+)
            numberPadding = "  ";  // 2 пробела
        }
        return $"{index + 1}{numberPadding}{algorithmSteps[index]};";
    }

    protected override void ValidateLineCount()
    {
        
    }

    protected override IEnumerator ExecuteAlgorithm()
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
            else if (step == "Взять")
            {
                ExecuteGetCommand();
            }
        }

        if (allItemsCollected && targetCheckPoint != null 
            && Vector3.Distance(playerTransform.position, targetCheckPoint.position) < 0.01f)
        {
            ShowCompletionDialog();
        }
        else
        {
            DialogeWindowBadEnd.SetActive(true);
        }

        isPlaying = false;
    }


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

    // Функция командды подбора объектов
    private void ExecuteGetCommand()
    {
        // Получаем масштаб канваса
        float scale = canvas.scaleFactor;
        float pickupDistance = 200f * scale;

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
                    item.SetActive(false);
                    collectedItemsCount++;

                    if (collectedItemsCount >= 4)
                    {
                        allItemsCollected = true;
                    }
                    break;
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) => HandleObstacleCollision(collision);

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
        if (checkPoints.Count > 0)
        {
            playerTransform.position = checkPoints[4].position;
            currentCheckPoint = checkPoints[4];
        }

        // Восстанавливаем предметы при сбросе алгоритма
        ResetItems();
    }

    private void ShowCompletionDialog()
    {
        if (DialogeWindowGoodEnd != null)
        {
            DialogeWindowGoodEnd.SetActive(true);
        }

        SaveLoadManager.SaveProgress(SceneManager.GetActiveScene().name);
    }
  
}