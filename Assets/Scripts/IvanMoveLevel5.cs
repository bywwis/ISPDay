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
}