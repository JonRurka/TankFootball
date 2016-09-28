using UnityEngine;
using System;
using System.Collections;

public class TurretScript : MonoBehaviour {
    public enum ShootMode {
        none,
        shortRange,
        Kickoff,
        pass,
    }

    public Transform gun;
    public ArcRender arc;
    public LayerMask mask;
    public Camera cam;
    public float launchVelocity = 10;
    public float velocityIncrease = 10;
    public float minLaunchVelocity = 10;
    public float maxLaunchVelocity = 1000;
    public float rotationSpeed = 5;
    public float maximumX = 5;
    public GameObject explosion;
    public AudioSource expSource;
    public Vector2 turretRotation;
    public ShootMode shootMode;
    public float rotX;
    public float angle;
    public bool Enabled = true;

    private Rigidbody body;
    private TankMovement movement;
    private Vector3 aimPoint;
    private Vector3 curAimPoint;
    private Vector3 screenPoint;
    private float lastSyncTime = 0f;
    private float syncDelay = 0f;
    private float syncTime = 0f;
    private Quaternion syncStartRotation;
    private Quaternion syncEndRotation;

    private float oldTurrentXrot;
    private Quaternion baseRotation;
    private Quaternion targetRot;
    private Quaternion oldQuat;

    void Awake() {
        movement = transform.root.GetComponent<TankMovement>();
    }

    // Use this for initialization
    void Start () {
        cam = GameObject.Find("Camera").GetComponent<Camera>();
        launchVelocity = (minLaunchVelocity + maxLaunchVelocity) / 2;
        baseRotation = gun.localRotation;
    }
	
	// Update is called once per frame
	void Update () {
        if (movement.isOwner) {
            Ray camRay = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2 + Screen.height / 5));
            RaycastHit camHit;
            if (Physics.Raycast(camRay, out camHit, 1000, mask.value)) {
                aimPoint = camHit.point;
            }

            //Debug.DrawRay(camRay.origin, camRay.direction * 1000, Color.red);

            Vector3 dir = (aimPoint - transform.position).normalized;
            Quaternion lookQuat = Quaternion.LookRotation(dir);
            lookQuat = Quaternion.Lerp(transform.rotation, lookQuat, Time.deltaTime * rotationSpeed);
            float rotY = lookQuat.eulerAngles.y;

            dir = (aimPoint - gun.position).normalized;
            lookQuat = Quaternion.LookRotation(dir);
            Quaternion q1 = Quaternion.Euler(lookQuat.eulerAngles.x, lookQuat.eulerAngles.y, 0);
            Quaternion q2 = Quaternion.Euler(0, lookQuat.eulerAngles.y, 0);
            angle = Quaternion.Angle(q1, q2);
            if (angle <= maximumX) {
                targetRot = lookQuat;
            }
            rotX = Quaternion.Lerp(gun.localRotation, targetRot, Time.deltaTime * rotationSpeed).eulerAngles.x;

            screenPoint = cam.WorldToScreenPoint(curAimPoint);
            MouseOrbitImproved.Instance.SetAimPoint(screenPoint);

            Ray gunRay = new Ray(gun.position, gun.forward);
            RaycastHit gunHit;
            if (Physics.Raycast(gunRay, out gunHit, 1000, mask)) {
                curAimPoint = gunHit.point;
            }

            if (Enabled) {
                transform.eulerAngles = new Vector3(0, rotY, 0);
                gun.localEulerAngles = new Vector3(rotX, 0, 0);
                turretRotation = new Vector2(rotX, rotY);
            }

            if (movement.HasBall && Enabled) {
                if (Input.GetKey(KeyCode.R))
                    launchVelocity += velocityIncrease * Time.deltaTime;
                if (Input.GetKey(KeyCode.F))
                    launchVelocity -= velocityIncrease * Time.deltaTime;

                if (launchVelocity <= minLaunchVelocity)
                    launchVelocity = minLaunchVelocity;
                if (launchVelocity >= maxLaunchVelocity)
                    launchVelocity = maxLaunchVelocity;

                if (turretRotation.x != oldTurrentXrot) {
                    oldTurrentXrot = turretRotation.x;
                    Vector3 LocalRightDir = gun.localRotation * Vector3.forward;
                    Vector3 velocity = new Vector3(0, LocalRightDir.y * launchVelocity, LocalRightDir.z * launchVelocity);
                    arc.Render(Vector3.zero, velocity, -Physics.gravity.y);
                }

                if (Input.GetKeyDown(KeyCode.Mouse0)) {
                    byte[] buffer = BitConverter.GetBytes(launchVelocity);
                    NetClient.Instance.Send(NetServer.OpCodes.Shoot, buffer);
                }
            }
            else if (Enabled) {
                if (Input.GetKeyDown(KeyCode.Mouse0)) {
                    NetClient.Instance.Send(NetServer.OpCodes.Shoot);
                }
            }



        }
        else {
            //Vector3 rot = Quaternion.Lerp(syncStartRotation, syncEndRotation, syncTime / syncDelay).eulerAngles;
            //transform.eulerAngles = new Vector3(0, rot.y, 0);
            //gun.localEulerAngles = new Vector3(rot.x, 0, 0);
        }
	}

    void OnGUI() {
        
    }

    public void GiveBall() {
        shootMode = ShootMode.pass;
    }

    public void RemoveBall() {
        if (movement.isOwner) {
            arc.Disable();
            shootMode = ShootMode.none;
        }
    }

    public void SetShootMode(ShootMode mode) {
        this.shootMode = mode;
    }

    public void SetTurretRotation(Vector2 rotation) {
        if (!movement.isOwner && Enabled) {
            /*syncTime = 0f;
            syncDelay = Time.time - lastSyncTime;
            lastSyncTime = Time.time;

            syncStartRotation = body.rotation;
            syncEndRotation = Quaternion.Euler(new Vector3(rotation.x, rotation.y, 0));*/

            turretRotation = rotation;
            transform.eulerAngles = new Vector3(0, rotation.y, 0);
            gun.localEulerAngles = new Vector3(rotation.x, 0, 0);
        }
    }

    public void ServerShoot(byte[] data) {
        NetServer.Instance.Send(NetClient.OpCodes.Shoot, new byte[] { movement.ID, (byte)shootMode });
        Ray gunRay;
        RaycastHit gunHit;
        switch (shootMode) {
            case ShootMode.Kickoff:
                gunRay = new Ray(gun.position, gun.forward);
                expSource.Play();
                if (Physics.Raycast(gunRay, out gunHit, 100)) {
                    Vector3 point = gunHit.point;
                    GameObject exp = (GameObject)Instantiate(explosion, point, Quaternion.identity);
                    Destroy(exp, 5);
                    if (gunHit.transform.tag == "Player") {
                        TankMovement enemyTank = gunHit.transform.root.GetComponent<TankMovement>();
                        if (enemyTank == null)
                            break;
                        GamePlay.Instance.Takle(movement.ID, enemyTank.ID);
                    }
                }
                break;

            case ShootMode.pass:
                if (movement.HasBall) {
                    Debug.Log("in Buffer:" + BitConverter.ToString(data));
                    float forwardVelocity = BitConverter.ToSingle(data, 0);
                    //Debug.Log(gun.localRotation * Vector3.forward);
                    //Vector3 LocalRightDir = gun.rotation * Vector3.forward;
                    //Vector3 velocity = new Vector3(0, LocalRightDir.y * forwardVelocity, LocalRightDir.z * forwardVelocity);
                    
                    if (forwardVelocity <= minLaunchVelocity)
                        forwardVelocity = minLaunchVelocity;
                    if (forwardVelocity >= maxLaunchVelocity)
                        forwardVelocity = maxLaunchVelocity;
                    Vector3 ballVelocity = new Vector3(0, 0, forwardVelocity);
                    GameBall.Instance.Fire(movement.m_Rigidbody.velocity, ballVelocity);
                }
                break;

            case ShootMode.shortRange:
                gunRay = new Ray(gun.position, gun.forward);
                if (Physics.Raycast(gunRay, out gunHit, 2, mask)) {
                    curAimPoint = gunHit.point;
                    if (gunHit.transform.tag == "Player") {
                        TankMovement enemyTank = gunHit.transform.root.GetComponent<TankMovement>();
                        if (enemyTank == null)
                            break;
                        GamePlay.Instance.Takle(movement.ID, enemyTank.ID);
                    }
                }
                break;
        }
    }

    public void ClientShootEffect(ShootMode mode) {
        Ray gunRay;
        RaycastHit gunHit;
        switch (mode) {
            case ShootMode.Kickoff:
                gunRay = new Ray(gun.position, gun.forward);
                expSource.Play();
                if (Physics.Raycast(gunRay, out gunHit, 100)) {
                    Vector3 point = gunHit.point;
                    GameObject exp = (GameObject)Instantiate(explosion, point, Quaternion.identity);
                    exp.GetComponent<ExplosionPhysicsForce>().IsClient = true;
                    Destroy(exp, 5);
                }
                break;

            case ShootMode.pass:
                break;

            case ShootMode.shortRange:
                break;
        }
    }

    public void SetEnabled(bool enabled) {
        Enabled = enabled;
    }

    public void ObjectCaught(GameObject obj) {
        if (obj.tag == "GameBall" && GameControl.Instance.IsServer) {
            
        }
    }
}
