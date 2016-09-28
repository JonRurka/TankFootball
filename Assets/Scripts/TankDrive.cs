using UnityEngine;
using System.Collections;

public class TankDrive : MonoBehaviour {
    public float driveForce = 20000;
    public float turnTorque = 20000;
    public float antiskidForce = 100000;
    public float jumpForce = 2000000;
    public float forwardSpeed = 20;
    public float reverseSpeed = 15;
    public float turnRate = 5;

    private float forwardSpeedGoal = 0.0f;
    private float turnSpeedGoal = 0.0f;

    Rigidbody body;

    void Start() {
        // Move the center of mass down for improved stability
        body = GetComponent<Rigidbody>();
        //body.centerOfMass.y -= 1.0;
    }

    void Update() {
        float speed = Vector3.Dot(body.velocity, transform.forward);

        var vertInput = Input.GetAxis("Vertical");
        forwardSpeedGoal = vertInput * (vertInput > 0 ? forwardSpeed : reverseSpeed);

        turnSpeedGoal = Input.GetAxis("Horizontal") * turnRate;
    }

    void FixedUpdate() {
        // control loop for forward speed
        var speed = Vector3.Dot(body.velocity, transform.forward);
        var error = forwardSpeedGoal - speed;
        body.AddRelativeForce(Vector3.forward * driveForce * error);

        // control loop to halt sideways slip
        var slip = Vector3.Dot(body.velocity, transform.right);
        error = -slip;
        body.AddRelativeForce(Vector3.right * antiskidForce * error);

        // control loop for turn rate
        var angVel = body.angularVelocity.y;
        error = turnSpeedGoal - angVel;
        body.AddRelativeTorque(Vector3.up * turnTorque * error);
    }
}
