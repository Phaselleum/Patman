using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class TruckBehaviour : MonoBehaviour
{
    [SerializeField] public Rigidbody2D car;
    [SerializeField] private Rigidbody2D wheelL;
    [SerializeField] private Rigidbody2D wheelR;
    [SerializeField] private WheelJoint2D wheelLJoint;
    [SerializeField] private WheelJoint2D wheelRJoint;
    [SerializeField] private JointMotor2D wheelLJointMotor;
    [SerializeField] private JointMotor2D wheelRJointMotor;
    [SerializeField] private float speed = 1f;
    [SerializeField] private WheelBehaviour wheelLBehaviour;
    [SerializeField] private WheelBehaviour wheelRBehaviour;

    [SerializeField] private bool moving;
    [SerializeField] private bool braking;
    [SerializeField] private Vector3 initialPosition = Vector3.zero;
    private Quaternion initialRotation = Quaternion.identity;
    public Vector3 checkpointOffset = Vector3.zero;

    private float deceleration = -400f;
    private float gravity = 9.8f;
    [SerializeField] private float angleCar = 0;
    [SerializeField] private float acceleration = 500f;
    [SerializeField] private float maxSpeed = 800f;
    [SerializeField] private float brakeforce = 1000f;

    [SerializeField] private AudioSource carSound;
    [SerializeField] private AudioSource brakeSound;

    [SerializeField] private GameObject mailTruck;
    [SerializeField] private GameObject mailLorry;
    [SerializeField] private GameObject mailCar;

    public bool isTouchingFlag;

    public static TruckBehaviour Instance;

    private void Awake()
    {
        Instance = this;
        initialPosition = transform.position - 11 * Vector3.up;
        //initialRotation = transform.rotation;
        //car.centerOfMass = new Vector2(0, 0f);

        wheelLJointMotor = wheelLJoint.motor;
        wheelRJointMotor = wheelRJoint.motor;
    }

    private void Update()
    {
        if(GrabberBehaviour.Instance.goalReached && 
           GrabberBehaviour.Instance.transform.position.y - transform.position.y < -7 && 
           !CanvasButtons.Instance.winScreen.activeSelf)
        {
            CanvasButtons.Instance.ShowWinScreen();
        }
    }

    private void FixedUpdate()
    {
        if (moving) MovementAction();
        else IdleAction();
        if(braking) BrakingAction();
        else UnBreakingAction();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.name == "flag") isTouchingFlag = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if(other.name == "flag") isTouchingFlag = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        //Checks for checkpoint or win condition (only if the vehicle reaches the point after the time has passed)
        if (TimerBarBehaviour.Instance.playbackMovement) return;
        if (TimerBarBehaviour.Instance.recordingMovement) return;
        if (CanvasButtons.Instance.winScreen.activeSelf) return;
        if (GrabberBehaviour.Instance.goalReached) return;
        if(other.name == "goal") SetCheckpoint(other.transform.position, true);
        if(other.name == "flag") SetCheckpoint(other.transform.position, false);
    }

    /// <summary>
    /// Handle movement playback commands (called from outside)
    /// </summary>
    /// <param name="movementState">Movement state to enter</param>
    public void Move(MovementState movementState)
    {
        switch (movementState)
        {
            case MovementState.MOVEMENT_START:
                if (!moving) PlayMoveAudio(true);
                moving = true;
                break;
            case MovementState.MOVEMENT_END:
                if (moving) PlayMoveAudio(false);
                moving = false;
                break;
            case MovementState.BRAKING_START:
                if (!braking) PlayBrakeAudio(true);
                braking = true;
                break;
            case MovementState.BRAKING_END:
                braking = false;
                break;
        }
    }

    /// <summary>
    /// Called at the end of movement playback. Resets motors and movement flags
    /// </summary>
    public void EndMovement()
    {
        moving = false;
        braking = false;
        wheelLJointMotor.motorSpeed = 0;
        wheelRJointMotor.motorSpeed = 0;
        wheelLJoint.motor = wheelLJointMotor;
        wheelRJoint.motor = wheelRJointMotor;
        UnBreakingAction();
        PlayMoveAudio(false);
    }

    /// <summary>
    /// Moves the vehicle through a combination of wheel motors and general forward force application
    /// </summary>
    private void MovementAction()
    {
        angleCar = transform.localEulerAngles.z;
        if (angleCar > 100) angleCar -= 360;
        
        if (wheelLBehaviour.isMakingContact)
        {
            wheelLJointMotor.motorSpeed = -Mathf.Clamp(wheelLJointMotor.motorSpeed - (acceleration - gravity 
                * Mathf.PI * (angleCar / 180) * 80) * Time.deltaTime, maxSpeed, -maxSpeed);
        }

        if (wheelRBehaviour.isMakingContact)
        {
            wheelRJointMotor.motorSpeed = -Mathf.Clamp(wheelRJointMotor.motorSpeed - (acceleration - gravity 
                * Mathf.PI * (angleCar / 180) * 80) * Time.deltaTime, maxSpeed, -maxSpeed);
        }

        if (wheelLBehaviour.isMakingContact && wheelRBehaviour.isMakingContact)
        {
            car.AddRelativeForce(speed * Time.fixedDeltaTime * transform.right);
        }

        wheelLJoint.motor = wheelLJointMotor;
        wheelRJoint.motor = wheelRJointMotor;

        //car.AddRelativeForce(transform.right * speed * Time.fixedDeltaTime);
    }

    private void PlayMoveAudio(bool play)
    {
        if(play)
        {
            carSound.time = 0;
            carSound.Play();
        }
        else
        {
            carSound.Stop();
        }
    }

    private void PlayBrakeAudio(bool play)
    {
        if(play)
        {
            brakeSound.time = 0;
            brakeSound.Play();
        }
        else
        {
            brakeSound.Stop();
        }
    }

    private void IdleAction()
    {
        wheelLJointMotor.motorSpeed = 0;
        wheelRJointMotor.motorSpeed = 0;
        wheelLJoint.motor = wheelLJointMotor;
        wheelRJoint.motor = wheelRJointMotor;
        /*if (!TimerBarBehaviour.Instance.playbackMovement) return;
        if (wheelLJointMotor.motorSpeed < 0)
        {
            wheelLJointMotor.motorSpeed = Mathf.Clamp(wheelLJointMotor.motorSpeed - (deceleration - gravity
                * Mathf.PI * (angleCar / 180) * 80) * Time.deltaTime, 0, -maxSpeed);
            wheelRJointMotor.motorSpeed = wheelLJointMotor.motorSpeed;
        } else if (wheelLJointMotor.motorSpeed > 0)
        {
            wheelLJointMotor.motorSpeed = Mathf.Clamp(wheelLJointMotor.motorSpeed - (-deceleration - gravity
                * Mathf.PI * (angleCar / 180) * 80) * Time.deltaTime, 0, -maxSpeed);
            wheelLJointMotor.motorSpeed = wheelRJointMotor.motorSpeed;
        }

        wheelLJoint.motor = wheelLJointMotor;
        wheelRJoint.motor = wheelRJointMotor;*/
    }

    /// <summary>
    /// Sets vehicle drag and wheel angular drag to actuate brakes
    /// </summary>
    private void BrakingAction()
    {
        if (wheelLBehaviour.isMakingContact && wheelRBehaviour.isMakingContact)
        {
            car.drag = 10;
        }
        wheelL.angularDrag = 10;
        wheelR.angularDrag = 10;
    }

    /// <summary>
    /// Called when breaks are released to return drag to default values
    /// </summary>
    private void UnBreakingAction()
    {
        car.drag = 0;
        wheelL.angularDrag = 2;
        wheelR.angularDrag = 2;
    }

    /// <summary>
    /// Resets all vehicle parts. Vehicle is placed at last checkpoint or scene start
    /// </summary>
    public void ResetPosition()
    {
        transform.position = initialPosition + 11.5f * Vector3.up;
        transform.rotation = initialRotation;
        car.velocity = Vector2.zero;
        car.rotation = 0;
        car.angularVelocity = 0;
        wheelL.velocity = Vector2.zero;
        wheelL.rotation = 0;
        wheelL.angularVelocity = 0;
        wheelR.velocity = Vector2.zero;
        wheelR.rotation = 0;
        wheelR.angularVelocity = 0;
        braking = false;
        moving = false;
        PlayMoveAudio(false);
    }

    public void ResetActiveMovement()
    {
        braking = false;
        moving = false;
    }

    /// <summary>
    /// Sets the reset position to a given position (for checkpoints)
    /// </summary>
    /// <param name="pos"></param>
    public void SetCheckpoint(Vector3 pos, bool isGoal)
    {
        if(!isGoal)
        {
            checkpointOffset = pos - initialPosition;
            initialPosition = pos;
            GrabberBehaviour.Instance.SetGrabberPositionOffset(checkpointOffset);
            CameraController.Instance.SetInitialCameraPosition(checkpointOffset);
        }
        else
        {
            checkpointOffset = pos - initialPosition;
            GrabberBehaviour.Instance.goalReached = true;
            GrabberBehaviour.Instance.GrabVehicle(checkpointOffset);
        }
    }

    public void SwitchToTruck()
    {
        mailTruck.SetActive(true);
        mailLorry.SetActive(false);
        mailCar.SetActive(false);
        GetComponent<Rigidbody2D>().mass = 8;
    }

    public void SwitchToLorry()
    {
        mailTruck.SetActive(false);
        mailLorry.SetActive(true);
        mailCar.SetActive(false);
        GetComponent<Rigidbody2D>().mass = 4;
    }

    public void SwitchToCar()
    {
        mailTruck.SetActive(false);
        mailLorry.SetActive(false);
        mailCar.SetActive(true);
        GetComponent<Rigidbody2D>().mass = 2;
    }
}