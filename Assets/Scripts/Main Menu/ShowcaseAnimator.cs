using System.Collections.Generic;
using UnityEngine;

public class ShowcaseAnimator : MonoBehaviour
{
    public void Initialize(RuntimeAnimatorController baseController, AnimationClip targetClip, float timeOffset = 0f)
    {
        if (baseController == null || targetClip == null)
        {
            Debug.LogWarning($"[ShowcaseAnimator] Missing base controller or target clip on {gameObject.name}");
            return;
        }

        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"[ShowcaseAnimator] No Animator found on {gameObject.name}");
            return;
        }

        AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController);

        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++)
        {
            overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, targetClip);
        }

        overrideController.ApplyOverrides(overrides);
        animator.runtimeAnimatorController = overrideController;

        if (timeOffset > 0f)
        {
            animator.Play(0, 0, timeOffset);
        }
    }
}
