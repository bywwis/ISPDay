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
}