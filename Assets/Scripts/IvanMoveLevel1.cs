using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class IvanMoveLevel1 : BaseMovementController
{
    [SerializeField] private List<Transform> checkPoints; // Список всех чекпоинтов

    public Canvas canvas;
    private Transform targetCheckPoint;  // Финальная точка

    protected override void Start()
    {
        // Вызов базовой инициализации
        base.Start();
        InitializeCheckpoints(); // Настройка точек маршрута
        InitializeItems(); // Активация системы сбора предметов
    }

    // Настройка точек маршрута
    private void InitializeCheckpoints()
    {
        if (checkPoints.Count > 0)
        {
            currentCheckPoint = checkPoints[4]; // Стартовая точка
            playerTransform.position = currentCheckPoint.position; // Установка позиции игрока
        }

        if (checkPoints.Count > 19) targetCheckPoint = checkPoints[19]; // Финишная точка

    }

    // Основная логика выполнения алгоритма
    protected override IEnumerator ExecuteAlgorithm()
    {
        for (int i = 0; i < algorithmSteps.Count; i++)
        {
            string step = algorithmSteps[i];
            Vector3 direction = GetDirectionFromStep(step);

            if (direction != Vector3.zero) // Для шагов движения
            {
                Transform nextCheckPoint = FindNextCheckPoint(direction);
                if (nextCheckPoint != null)
                {
                    yield return StartCoroutine(MovePlayer(nextCheckPoint.position));
                    currentCheckPoint = nextCheckPoint;
                }
            }
            else if (step == "Взять") // Для команды сбора
            {
                ExecuteGetCommand();
            }
        }

        // Проверка условий завершения уровня
        CheckLevelCompletion();
        isPlaying = false;
    }

    private void CheckLevelCompletion()
    {
        if (allItemsCollected && targetCheckPoint != null
            && Vector3.Distance(playerTransform.position, targetCheckPoint.position) < 0.01f)
        {
            ShowCompletionDialog();
        }
        else
        {
            ShowBadEndDialog();
        }
    }

    // Поиск следующей точки в направлении движения
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

    // Обработчики столкновений
    private void OnTriggerEnter2D(Collider2D collision) { HandleObstacleCollision(collision); }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            isPathBlocked = false;
        }
    }

    // Сброс состояния
    public override void StopAlgorithm()
    {
        base.StopAlgorithm();
        InitializeCheckpoints();
        // Восстанавливаем предметы при сбросе алгоритма
        ResetItems();
    }
  
}