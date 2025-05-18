using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class IvanMoveLevel4 : CycleMovementController
{
    protected override int GetInitialCheckpointIndex() => 18;
    protected override int GetTargetCheckpointIndex() => 70;
    protected override string GetWrongItemTag() => "wrongChair";
    protected override int GetRequiredItemsCount() => 1;
    protected override int GetMaxStepsWithoutCycle() => 10;
    protected override int GetMaxStepsWithCycle() => 26;
}