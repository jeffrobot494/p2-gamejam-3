using UnityEngine;
using EpsilonIV;

[RequireComponent(typeof(PlayerCharacterController))]
public class PlayerAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource footstepsSource;
    public AudioSource actionSource;

    [Header("Footstep Settings")]
    public AudioClip[] walkClips;
    public AudioClip[] runClips;
    public AudioClip[] crouchClips;
    public float walkStepRate = 0.5f;
    public float runStepRate = 0.3f;
    public float crouchStepRate = 0.7f;

    [Header("Action Clips")]
    public AudioClip jumpClip;
    public AudioClip landClip;
    public AudioClip fallDamageClip;

    private PlayerCharacterController controller;
    private MovementState currentState;

    private float footstepTimer;
    public float footstepCooldown = 1f; // seconds
    private float lastFootstepTime = -1f;


    void Awake()
    {
        controller = GetComponent<PlayerCharacterController>();
    }

    void OnEnable()
    {
        controller.OnMovementStateChanged += OnMovementStateChanged;
        controller.OnJumped += OnJumped;
        controller.OnLanded += OnLanded;
        controller.OnFallDamage += OnFallDamage;
    }

    void OnDisable()
    {
        controller.OnMovementStateChanged -= OnMovementStateChanged;
        controller.OnJumped -= OnJumped;
        controller.OnLanded -= OnLanded;
        controller.OnFallDamage -= OnFallDamage;
    }

    void Update()
    {
        HandleFootsteps();
    }

    private void OnMovementStateChanged(MovementState newState)
    {
        currentState = newState;
        footstepTimer = 0f; // reset footstep rhythm when changing states
    }

    private void HandleFootsteps()
    {
        // Only play footsteps while grounded and moving
        if (currentState == MovementState.Idle || currentState == MovementState.InAir)
            return;

        footstepTimer -= Time.deltaTime;

        float rate = walkStepRate;
        float amplitude = 1;
        AudioClip[] clips = walkClips;

        switch (currentState)
        {
            case MovementState.Running:
                rate = runStepRate;
                clips = runClips.Length > 0 ? runClips : walkClips;
                amplitude = 1.5f;
                break;
            case MovementState.CrouchWalking:
                rate = crouchStepRate;
                clips = crouchClips.Length > 0 ? crouchClips : walkClips;
                amplitude = 0.5f;
                break;
        }

        if (footstepTimer <= 0f)
        {
            if (Time.time - lastFootstepTime < footstepCooldown * rate)
                return;
                
            PlayFootstep(clips, amplitude);
            footstepTimer = rate;
            lastFootstepTime = Time.time;
        }
    }

    private void PlayFootstep(AudioClip[] clips, float amplitude)
    {
        if (clips.Length == 0 || footstepsSource == null) return;
        var clip = clips[Random.Range(0, clips.Length)];
        footstepsSource.pitch = Random.Range(0.95f, 1.05f);
        footstepsSource.PlayOneShot(clip, amplitude);


    }

    private void OnJumped() => PlayActionSound(jumpClip);
    private void OnLanded() => PlayActionSound(landClip);
    private void OnFallDamage() => PlayActionSound(fallDamageClip);

    private void PlayActionSound(AudioClip clip)
    {
        if (clip == null || actionSource == null) return;
        actionSource.pitch = Random.Range(0.9f, 1.1f);
        actionSource.PlayOneShot(clip);
    }
}
