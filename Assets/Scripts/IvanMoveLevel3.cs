using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class IvanMoveLevel3 : CycleMovementController
{
    protected override int GetInitialCheckpointIndex() => 9;
    protected override int GetTargetCheckpointIndex() => 31;
    protected override string GetWrongItemTag() => "fish";

    protected override void CheckLevelCompletion()
    {
        bool isAtTarget = targetCheckPoint != null &&
                         Vector3.Distance(playerTransform.position, targetCheckPoint.position) < 0.1f;

        if (isAtTarget && !hasWrongItem && collectedItemsCount >= 3)
        {
            ShowCompletionDialog();
        }
        else
        {
            if (DialogeWindowBadEnd != null)
            {
                DialogeWindowBadEnd.SetActive(true);
            }
        }
    }
}