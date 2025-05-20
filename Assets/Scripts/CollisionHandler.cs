using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    [SerializeField] private ConditionalMovementController mainController;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & mainController.ObstacleLayer) != 0)
        {
            mainController.HandleCollision(gameObject.name);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & mainController.ObstacleLayer) != 0)
        {
            mainController.ClearCollision();
        }
    }
}

