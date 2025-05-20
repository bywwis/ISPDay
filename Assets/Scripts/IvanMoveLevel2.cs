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

    protected override void Start()
    {
        storyMessages = new string[] {
            "По пути в аудиторию Иван встретил свою подругу Паулину. Они разговорились, и Иван узнал, что Паулина любит ничего не делать и " +
            "смотреть в оĸно на леĸции, а Иван, наоборот, любит слушать преподавателя и писать ĸонспеĸт.",
            "Нужно помочь Ивану и Паулине сесть на верные места."
        };

        base.Start();
        ShowStoryDialog();
    }


    protected override void CheckLevelCompletion()
    {
        if (currentIvanCheckPoint == checkPoints[GetIvanTargetCheckpointIndex()] &&
            currentPaulinaCheckPoint == checkPoints[GetPaulinaTargetCheckpointIndex()])
        {
            ShowCompletionDialog($"Паулина и Иван верно сидят на своих местах. Теперь нужно 1,5 часа слушать лекцию.");
        }
        else if (currentIvanCheckPoint == checkPoints[GetIvanTargetCheckpointIndex()] &&
           currentPaulinaCheckPoint != checkPoints[GetPaulinaTargetCheckpointIndex()])
        {
            ShowBadEndDialog($"Ты выбрал правильное место для Ивана, но Паулине не нравится место, на котором она сидит. Попробуй ещё раз!");
        }
        else if (currentIvanCheckPoint != checkPoints[GetIvanTargetCheckpointIndex()] &&
          currentPaulinaCheckPoint == checkPoints[GetPaulinaTargetCheckpointIndex()])
        {
            ShowBadEndDialog($"Ты выбрал правильное место для Паулины, но Ивану не нравится место, на котором он сидит. Попробуй ещё раз!");
        }
        else
        {
            ShowBadEndDialog($"Иван и Паулина недовольны своими местами. Попробуй ещё раз!");
        }
    }
}