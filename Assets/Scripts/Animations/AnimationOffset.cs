using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationOffset : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float startOffset;

    [SerializeField] private float animationSpeed = 1f;

    private void Start()
    {
        Animator animator = GetComponent<Animator>();

        animator.speed = animationSpeed;
        animator.Play(0, 0, startOffset);
    }
}