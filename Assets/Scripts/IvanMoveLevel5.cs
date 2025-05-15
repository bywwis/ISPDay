using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class IvanMoveLevel5 : ConditionalMovementController
{
    protected override int GetIvanInitialCheckpointIndex() => 55;
    protected override int GetIvanTargetCheckpointIndex() => 86;
    protected override int GetPaulinaInitialCheckpointIndex() => 43;
    protected override int GetPaulinaTargetCheckpointIndex() => 35;
}