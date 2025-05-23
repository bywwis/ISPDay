using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class IvanMoveLevel1 : BaseMovementController
{
    protected override int GetInitialCheckpointIndex() => 4;
    protected override int GetTargetCheckpointIndex() => 19;

    protected override void Start()
    {
        storyMessages = new string[] {
            "Добро пожаловать в мир Ивана, обычного студента, который учится на специальности «Информационные системы и программирование»." +
            " Его жизнь полна ежедневных вызовов, от утренней спешки до работы над сложными проектами.",
            "В течение дня тебя ждет множество ситуаций, в которых твое участие будет бесценным. Приготовься к насыщенному «Дню из жизни студента»!",
            "Сегодняшний день начинается, как и многие другие, – с пропущенного будильника. Комната превратилась в хаотичный лабиринт из разбросанных " +
            "вещей. Нужно быстро собрать вещи и скорее спешить на лекцию!",
            "Для управления Иваном необходимо использовать команды и перемещаться по комнате с помощью составленного алгоритма из набора команд."
        };

        // Вызов базовой инициализации
        base.Start();
        InitializeItems(); // Активация системы сбора предметов
        ShowStoryDialog();
    }

    // Сброс состояния
    public override void StopAlgorithm()
    {
        base.StopAlgorithm();
        // Восстанавливаем предметы при сбросе алгоритма
        ResetItems();
    }

    protected override void CheckLevelCompletion()
    {
        if (allItemsCollected && currentCheckPoint == targetCheckPoint)
        {
            ShowCompletionDialog($"Благодаря тебе Иван успел собраться! Скорее спешим в лекционную аудиторию!");
        }
        else if (collectedItemsCount < 4 && currentCheckPoint == targetCheckPoint)
        {
            ShowBadEndDialog($"О нет, Иван не собрал всё, что надо.");
        }
        else if (allItemsCollected && currentCheckPoint != targetCheckPoint)
        {
            ShowBadEndDialog($"О нет, ты забыл вывести Ивана из комнаты.");
        }
        else
        {
            ShowBadEndDialog($"О нет, тебе не удалось помочь Ивану быстро собраться. Теперь ему придётся пропустить лекцию, а ведь у " +
                $"него была такая тяга к новым знаниям. Попробуй ещё раз!");
        }
    }
}