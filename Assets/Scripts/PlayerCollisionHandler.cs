// PlayerCollisionHandler.cs (висит на префабе Ивана/Паулины)
using UnityEngine;

public class PlayerCollisionHandler : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleLayer;
    private MazeLevel levelManager; // ссылка на основной скрипт

    private void Start()
    {
        // Находим основной скрипт (если он в сцене)
        levelManager = FindObjectOfType<MazeLevel>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            Debug.Log($"Персонаж {name} столкнулся с {collision.gameObject.name}");
            if (levelManager != null)
                levelManager.ReportCollision(true); // сообщаем основному скрипту
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            if (levelManager != null)
                levelManager.ReportCollision(false);
        }
    }
}