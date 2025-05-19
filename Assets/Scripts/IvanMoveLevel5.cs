using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class IvanMoveLevel5 : ConditionalMovementController
{
    protected override int GetIvanInitialCheckpointIndex() => 90;
    protected override int GetIvanTargetCheckpointIndex() => 65;
    protected override int GetPaulinaInitialCheckpointIndex() => 24;
    protected override int GetPaulinaTargetCheckpointIndex() => 75;

    protected override void CheckLevelCompletion()
    {
        if (currentIvanCheckPoint == checkPoints[GetIvanTargetCheckpointIndex()] &&
            currentPaulinaCheckPoint == checkPoints[GetPaulinaTargetCheckpointIndex()])
        {
            // Показываем диалоговое окно для успешного прохождения уровня
            if (DialogeWindowGoodEnd != null)
            {
                ShowCompletionDialog($"Паулина и Иван верно сидят на своих местах. Теперь нужно 1,5 часа слушать лекцию.");
            }
        }
        else
        {
            ShowBadEndDialog();
        }
    }
}