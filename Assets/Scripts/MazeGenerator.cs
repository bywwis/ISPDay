using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Sprite[] backgroundSprites;
    [SerializeField] private Sprite obstacleSprite;
    [SerializeField] private GameObject checkpointPrefab;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject finishPrefab;
    [SerializeField] private GameObject backgroundPrefab; // Добавляем префаб фона
    
    [SerializeField] private int mazeWidth = 10;
    [SerializeField] private int mazeHeight = 10;
    [SerializeField] private float cellSize = 1f;
    
    private GameObject backgroundInstance;
    private List<GameObject> checkpoints = new List<GameObject>();
    private List<GameObject> obstacles = new List<GameObject>();
    private GameObject playerInstance;
    private GameObject finishInstance;

    public void GenerateMaze(int width, int height)
    {
        mazeWidth = width;
        mazeHeight = height;
        
        ClearMaze();
        CreateBackground();
        GenerateCheckpoints();
        GenerateObstacles();
        PlacePlayer();
        PlaceFinish();
    }

    private void CreateBackground()
    {
        // Создаём фон из префаба
        backgroundInstance = Instantiate(backgroundPrefab, Vector3.zero, Quaternion.identity);
        
        // Центрируем фон
        Vector3 centerPos = new Vector3(
            (mazeWidth * cellSize - cellSize) / 2f,
            (mazeHeight * cellSize - cellSize) / 2f,
            10f); // Z=10 для заднего плана
            
        backgroundInstance.transform.position = centerPos;
        
        // Масштабируем под размер лабиринта
        SpriteRenderer renderer = backgroundInstance.GetComponent<SpriteRenderer>();
        if(renderer != null)
        {
            Vector3 scale = backgroundInstance.transform.localScale;
            scale.x = mazeWidth * cellSize / renderer.sprite.bounds.size.x;
            scale.y = mazeHeight * cellSize / renderer.sprite.bounds.size.y;
            backgroundInstance.transform.localScale = scale;
        }
    }

    private void GenerateCheckpoints()
    {
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                Vector3 position = new Vector3(x * cellSize, y * cellSize, 0);
                GameObject checkpoint = Instantiate(checkpointPrefab, position, Quaternion.identity);
                checkpoint.name = $"Checkpoint_{x}_{y}";
                checkpoints.Add(checkpoint);
            }
        }
    }

    private void GenerateObstacles()
    {
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                if (Random.value < 0.3f && !(x == 0 && y == 0))
                {
                    Vector3 position = new Vector3(x * cellSize, y * cellSize, 0);
                    GameObject obstacle = new GameObject($"Obstacle_{x}_{y}");
                    obstacle.transform.position = position;
                    
                    SpriteRenderer renderer = obstacle.AddComponent<SpriteRenderer>();
                    renderer.sprite = obstacleSprite;
                    renderer.sortingLayerName = "Obstacles";
                    
                    BoxCollider2D collider = obstacle.AddComponent<BoxCollider2D>();
                    collider.isTrigger = true;
                    obstacle.layer = LayerMask.NameToLayer("Obstacles");
                    
                    obstacles.Add(obstacle);
                }
            }
        }
    }

    private void PlacePlayer()
    {
        if(playerInstance == null)
        {
            Vector3 startPosition = new Vector3(0, 0, 0);
            playerInstance = Instantiate(playerPrefab, startPosition, Quaternion.identity);
        }
        else
        {
            playerInstance.transform.position = new Vector3(0, 0, 0);
        }
    }

    private void PlaceFinish()
    {
        if(finishInstance == null)
        {
            Vector3 finishPosition = new Vector3(
                (mazeWidth-1) * cellSize, 
                (mazeHeight-1) * cellSize, 
                0);
            finishInstance = Instantiate(finishPrefab, finishPosition, Quaternion.identity);
        }
        else
        {
            finishInstance.transform.position = new Vector3(
                (mazeWidth-1) * cellSize, 
                (mazeHeight-1) * cellSize, 
                0);
        }
    }

    public void ClearMaze()
    {
        if (backgroundInstance != null) Destroy(backgroundInstance);
        foreach (var checkpoint in checkpoints) Destroy(checkpoint);
        foreach (var obstacle in obstacles) Destroy(obstacle);
        
        // Не удаляем игрока и финиш, а только перемещаем
        if (playerInstance != null) playerInstance.transform.position = new Vector3(0, 0, 0);
        if (finishInstance != null) 
        {
            finishInstance.transform.position = new Vector3(
                (mazeWidth-1) * cellSize, 
                (mazeHeight-1) * cellSize, 
                0);
        }
        
        checkpoints.Clear();
        obstacles.Clear();
    }
}