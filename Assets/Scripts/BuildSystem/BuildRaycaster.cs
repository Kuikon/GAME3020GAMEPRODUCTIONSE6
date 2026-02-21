using UnityEngine;
using UnityEngine.InputSystem;

public class BuildRaycaster
{
    private readonly Camera cam;
    private readonly float rayDistance;
    private readonly LayerMask placeMask;
    private readonly LayerMask blockOnlyMask;
    public BuildRaycaster(Camera cam, float rayDistance = 200f,
        LayerMask placeMask = default, LayerMask blockOnlyMask = default)
    {
        this.cam = cam;
        this.rayDistance = rayDistance;
        this.placeMask = placeMask;
        this.blockOnlyMask = blockOnlyMask;
    }

    private Ray MakeMouseRay()
    {
        return cam.ScreenPointToRay(Mouse.current.position.ReadValue());
    }

    public bool RaycastForPlace(out RaycastHit hit)
    {
        hit = default;
        if (cam == null || Mouse.current == null) return false;

        Ray ray = MakeMouseRay();
        Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.red, 0.05f);
        return Physics.Raycast(ray, out hit, rayDistance, placeMask, QueryTriggerInteraction.Ignore);
    }

    public bool RaycastForRemove(out RaycastHit hit)
    {
        hit = default;
        if (cam == null || Mouse.current == null) return false;

        Ray ray = MakeMouseRay();
        Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.blue, 0.05f);
        return Physics.Raycast(ray, out hit, rayDistance, blockOnlyMask, QueryTriggerInteraction.Ignore);
    }
}