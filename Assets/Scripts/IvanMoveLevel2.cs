using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class IvanMoveLevel2 : ConditionalMovementController
{
    protected override int GetIvanInitialCheckpointIndex() => 55;
    protected override int GetIvanTargetCheckpointIndex() => 86;
    protected override int GetPaulinaInitialCheckpointIndex() => 43;
    protected override int GetPaulinaTargetCheckpointIndex() => 35;

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