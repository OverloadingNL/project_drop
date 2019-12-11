using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Helicopter : MonoBehaviour
{
    [SerializeField] RectTransform fuelBar = null;
    [SerializeField] float maxFuelMass = 800;              // [kg]

    [Range(0, 1f)]
    [SerializeField]
    float thrustPercent;         // [none]
    [SerializeField] float rcsThrust = 50f;
    [SerializeField] float mainThrust = 100f;
    [SerializeField] float rotationLimitDegree = 10f;
    [SerializeField] float levelLoadDelay = 2f;

    [SerializeField] AudioClip mainEngine;
    [SerializeField] AudioClip success;
    [SerializeField] AudioClip death;

    [SerializeField] ParticleSystem mainEngineParticles;
    [SerializeField] ParticleSystem successParticles;
    [SerializeField] ParticleSystem deathParticles;

    Rigidbody rigidBody;
    AudioSource audioSource;
    Rotator[] rotators;

    bool isTransitioning = false;
    bool collisionsDisabled = false;

    float currentFuelMass;              // [kg]
    float currentThrust;        // N  <- Note Newtons NOT kN

    float x = 0.0f;
    float y = 0.0f;
    float z = 0.0f;

    float fuelAsPercentage { get { return currentFuelMass / maxFuelMass; } }

    // Use this for initialization
    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        rotators = GetComponentsInChildren<Rotator>();

        currentFuelMass = maxFuelMass;

        if (!audioSource.isPlaying) // so it doesn't layer
        {
            audioSource.clip = mainEngine;
            audioSource.Play();
        }

        SetRotationValues();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isTransitioning)
        {
            RespondToThrustInput();
            RespondToRotateInput();
        }
        if (Debug.isDebugBuild)
        {
            RespondToDebugKeys();
        }

        var pos = transform.position;
        pos.y = Mathf.Clamp(transform.position.y, 0f, 80f);
        transform.position = pos;
        UpdateFuelBar();
    }

    void UpdateFuelBar()
    {
        if (fuelBar)
        {
            fuelBar.localScale = new Vector3(1f, fuelAsPercentage, 1f);
        }
    }

    void SetRotationValues()
    {
        x = transform.eulerAngles.x;
        y = transform.eulerAngles.y;
        z = transform.eulerAngles.z;
    }

    private void RespondToDebugKeys()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadNextLevel();
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            collisionsDisabled = !collisionsDisabled; // toggle
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            currentFuelMass = maxFuelMass;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isTransitioning || collisionsDisabled) { return; }

        switch (collision.gameObject.tag)
        {
            case "Friendly":
                // do nothing
                break;
            case "Finish":
                StartSuccessSequence();
                break;
            default:
                StartDeathSequence();
                break;
        }
    }

    private void StartSuccessSequence()
    {
        isTransitioning = true;
        audioSource.Stop();
        audioSource.PlayOneShot(success);
        if (successParticles != null)
        {
            successParticles.Play();
        }
        Invoke("LoadNextLevel", success.length + levelLoadDelay);
    }

    private void StartDeathSequence()
    {
        StopApplyingThrust();
        isTransitioning = true;
        audioSource.Stop();
        audioSource.PlayOneShot(death);
        if (deathParticles != null)
        {
            deathParticles.Play();
        }
        Invoke("LoadFirstLevel", levelLoadDelay);
    }

    private void LoadNextLevel()
    {
        GameManager.instance.LoadNextLevel();
    }

    private void LoadFirstLevel()
    {
        GameManager.instance.LoadFirstLevel();
    }

    private void RespondToThrustInput()
    {
        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.touchCount > 0 || Input.GetMouseButton(0)) // can thrust while rotating
        {
            ApplyThrust();
        }
        else
        {
            StopApplyingThrust();
        }
    }

    private void StopApplyingThrust()
    {
        audioSource.pitch = 0.4f;
        mainEngineParticles.Stop();
        ChangeRotatorSpeed(30);
    }

    private void ApplyThrust()
    {
        if (currentFuelMass > 0)
        {
            currentFuelMass -= FuelThisUpdate();
            rigidBody.AddRelativeForce(Vector3.up * mainThrust * Time.deltaTime);
            ChangeRotatorSpeed(100);
            audioSource.pitch = 0.8f;
            mainEngineParticles.Play();
            ExertForce();
        }
        else
        {
            StopApplyingThrust();
        }
    }

    private void ChangeRotatorSpeed(int rpm)
    {
        foreach (Rotator rotator in rotators)
        {
            rotator.zRotationsPerMinute = rpm;
        }
    }

    void RespondToRotateInput()
    {
        if (Input.GetKey(KeyCode.A))
        {
            x -= rcsThrust * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.D))
        {
            x += rcsThrust * Time.deltaTime;
        }

        if (Input.GetMouseButton(0))
        {
            var playerScreenPoint = Camera.main.WorldToScreenPoint(transform.position);
            var mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (mouse.y < playerScreenPoint.y)
            {
                x -= rcsThrust * Time.deltaTime;
            }
            else
            {
                x += rcsThrust * Time.deltaTime;
            }
        }

        x = ClampAngle(x, -rotationLimitDegree, rotationLimitDegree);

        Quaternion newRotation = Quaternion.Euler(x, y, z);

        transform.rotation = newRotation;
    }

    float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;

        return Mathf.Clamp(angle, min, max);
    }

    float FuelThisUpdate()
    {                           // [
        float exhaustMassFlow;                          // [
        float effectiveExhastVelocity;                  // [

        effectiveExhastVelocity = 4462f;                // [m s^-1]  liquid H O
        exhaustMassFlow = currentThrust / effectiveExhastVelocity;

        return exhaustMassFlow * Time.deltaTime;
    }

    void ExertForce()
    {
        currentThrust = thrustPercent * mainThrust * 1000f;
    }

}