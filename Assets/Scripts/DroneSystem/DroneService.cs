using System;
using UnityEngine;

public sealed class DroneService
{
    private readonly DroneCompanionController controller;

    public bool IsBusy => controller != null && controller.IsBusy;
    public bool IsCarrying => controller != null && controller.IsCarrying;

    public event Action SequenceFinished;

    public DroneService(DroneCompanionController controller)
    {
        this.controller = controller;

        if (this.controller != null)
            this.controller.SequenceFinished += HandleControllerSequenceFinished;
    }

    private void HandleControllerSequenceFinished()
    {
        SequenceFinished?.Invoke();
    }

    public void SetIdle()
    {
        if (controller == null)
            return;

        controller.SetIdle();
    }

    public void ReactTo(Vector3 worldPos)
    {
        if (controller == null)
            return;

        controller.SetReactTarget(worldPos);
    }

    public void PlayBuild(GameObject target)
    {
        if (controller == null || target == null)
            return;

        controller.PlayBuild(target);
    }

    public void PlayBuildAt(Vector3 worldPos)
    {
        if (controller == null)
            return;

        controller.PlayBuildAt(worldPos);
    }

    public void PlayRemove(Transform target)
    {
        if (controller == null || target == null)
            return;

        controller.PlayRemove(target);
    }

    public void BeginCarry(Transform target)
    {
        if (controller == null || target == null)
            return;

        controller.BeginCarry(target);
    }

    public void CommitCarry(Transform target)
    {
        if (controller == null)
            return;

        controller.CommitCarry(target);
    }

    public void CancelCarry()
    {
        if (controller == null)
            return;

        controller.CancelCarry();
    }
}