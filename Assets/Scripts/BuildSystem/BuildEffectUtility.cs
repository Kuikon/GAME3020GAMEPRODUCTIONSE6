using UnityEngine;

public static class BuildEffectUtility
{
    public static void PlayBuildEffect(GameObject spawnedObject)
    {
        if (spawnedObject == null)
            return;

        BlockBuildEffect effect = spawnedObject.GetComponent<BlockBuildEffect>();

        if (effect == null)
            effect = spawnedObject.AddComponent<BlockBuildEffect>();

        effect.PlayBuild();
    }
    public static void PlayDestroyEffect(GameObject targetObject, System.Action onComplete = null)
    {
        if (targetObject == null)
        {
            onComplete?.Invoke();
            return;
        }

        BlockBuildEffect effect = targetObject.GetComponent<BlockBuildEffect>();

        if (effect == null)
            effect = targetObject.AddComponent<BlockBuildEffect>();

        effect.PlayDestroy(onComplete);
    }
}