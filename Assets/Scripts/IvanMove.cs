using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IvanMove : MonoBehaviour
{
    [SerializeField]
    float moveSpeed = 2f;

    [SerializeField]
    private List<Transform> checkPoints; // Список всех чекпоинтов

    [SerializeField]
    private LayerMask obstacleLayer; // Слой для объектов, которые блокируют движение

    private Transform currentCheckPoint; // Текущий чекпоинт
    private bool isMoving = false; // Флаг для проверки движения
    private Vector3 targetPosition; // Позиция, к которой движется персонаж

    void Start()
    {
        // Устанавливаем начальный чекпоинт (например, первый в списке)
        if (checkPoints.Count > 0)
        {
            currentCheckPoint = checkPoints[0];
            transform.position = currentCheckPoint.position;
        }
    }

    void Update()
    {
        if (isMoving)
        {
            MoveToTarget();
        }
    }

    void MoveToTarget()
    {
        // Двигаем персонажа к целевой позиции
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // Если персонаж достиг целевой позиции, останавливаем движение
        if (transform.position == targetPosition)
        {
            isMoving = false;
            currentCheckPoint = FindCheckPointAtPosition(targetPosition); // Обновляем текущий чекпоинт
        }
    }

    // Находим чекпоинт по позиции
    Transform FindCheckPointAtPosition(Vector3 position)
    {
        foreach (var checkPoint in checkPoints)
        {
            if (checkPoint.position == position)
            {
                return checkPoint;
            }
        }
        return null;
    }

    // Находим ближайший чекпоинт в заданном направлении
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

    // Методы для кнопок (должны быть public, чтобы их можно было вызвать из UI)
    public void MoveRight()
    {
        if (!isMoving)
        {
            Transform nextCheckPoint = FindNextCheckPoint(Vector3.right);
            if (nextCheckPoint != null)
            {
                targetPosition = nextCheckPoint.position;
                isMoving = true;
            }
        }
    }

    public void MoveLeft()
    {
        if (!isMoving)
        {
            Transform nextCheckPoint = FindNextCheckPoint(Vector3.left);
            if (nextCheckPoint != null)
            {
                targetPosition = nextCheckPoint.position;
                isMoving = true;
            }
        }
    }

    public void MoveUp()
    {
        if (!isMoving)
        {
            Transform nextCheckPoint = FindNextCheckPoint(Vector3.up);
            if (nextCheckPoint != null)
            {
                targetPosition = nextCheckPoint.position;
                isMoving = true;
            }
        }
    }

    public void MoveDown()
    {
        if (!isMoving)
        {
            Transform nextCheckPoint = FindNextCheckPoint(Vector3.down);
            if (nextCheckPoint != null)
            {
                targetPosition = nextCheckPoint.position;
                isMoving = true;
            }
        }
    }
}