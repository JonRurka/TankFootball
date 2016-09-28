using UnityEngine;
using System.Collections;

[AddComponentMenu("Camera-Control/Mouse Orbit with zoom")]
public class MouseOrbitImproved : MonoBehaviour {
    public static MouseOrbitImproved Instance;

    public Texture2D crosshair;
    public Texture2D dot;
    public Transform target;
    public LayerMask mask;
    public Vector3 offset;
    public float distance = 5.0f;
    public float xSpeed = 120.0f;
    public float ySpeed = 120.0f;
    public bool inputEnabled = true;
    public bool mouseClickEnabled = true;
    public bool freeCam = false;
    public Vector3 localAnchor = new Vector3(0, 2, 3);

    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;

    public float distanceMin = .5f;
    public float distanceMax = 15f;

    public float chaseHeight = 3.0f;
    public float chaseDamping = 5.0f;
    public bool chaseSmoothRotation = true;
    public bool chaseFollowBehind = true;
    public float chaseRotationDamping = 10.0f;

    private bool transferingToFree = false;
    private float transferT = 0f;

    private Rigidbody _rigidbody;
    private TurretScript tankInst;
    private float targetDist;

    float x = 0.0f;
    float y = 0.0f;

    Vector3 aimScreenPoint = Vector3.zero;

    void Awake() {
        Instance = this;
    }

    // Use this for initialization
    void Start() {
        targetDist = distance;
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        _rigidbody = GetComponent<Rigidbody>();

        // Make the rigid body not change rotation
        if (_rigidbody != null) {
            _rigidbody.freezeRotation = true;
        }
    }

    void LateUpdate() {
        if (mouseClickEnabled && Input.GetKeyDown(KeyCode.Mouse0)) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            inputEnabled = true;
        }

        if (mouseClickEnabled && Input.GetKeyDown(KeyCode.Escape)) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            inputEnabled = false;
        }

        if (mouseClickEnabled && Input.GetMouseButtonDown(1)) {
            SetMode(true);
        }

        if (mouseClickEnabled && Input.GetMouseButtonUp(1)) {
            SetMode(false);
        }

        if (target) {

            if (freeCam) {
                float inX = 0;
                float inY = 0;
                if (inputEnabled) {
                    inX = Input.GetAxis("Mouse X");
                    inY = Input.GetAxis("Mouse Y");
                }

                if (transform.position.y < 0.0f && inY >= 0)
                    inY = -1;
                else if (transform.position.y < 0.2f && inY >= 0)
                    inY = 0;

                x += inX * xSpeed * distance * 0.02f * Time.deltaTime;
                y -= inY * ySpeed * 0.02f * Time.deltaTime;


                y = ClampAngle(y, yMinLimit, yMaxLimit);

                Quaternion rotation = Quaternion.Euler(y, x, 0);

                distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * 5, distanceMin, distanceMax);

                float diff = distanceMax - distanceMin;
                float max = distanceMin + diff / 8;
                float t = Mathf.InverseLerp(max, distanceMin, distance);
                Vector3 globalOffset = target.TransformVector(new Vector3(offset.x * t, offset.y, 0));
                Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
                Vector3 position = rotation * negDistance + (target.position + globalOffset);

                if (transferingToFree) {
                    transferT += Time.deltaTime * chaseRotationDamping;
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotation, transferT);
                    transform.position = Vector3.Lerp(transform.position, position, transferT);
                    if (transferT >= 1f) {
                        transferingToFree = false;
                    }
                }
                else {
                    transform.rotation = rotation;
                    transform.position = position;
                }
            }
            else {
                distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * 5, distanceMin, distanceMax);
                int dir = chaseFollowBehind ? -1 : 1;
                Vector3 lpos = target.parent.InverseTransformPoint(transform.position);
                float sx = Mathf.Lerp(lpos.x, 0, Time.deltaTime * chaseDamping);
                transform.position = target.parent.TransformPoint(sx, chaseHeight, distance * dir);
                if (chaseSmoothRotation) {
                    Quaternion wantedRot = Quaternion.LookRotation(target.position - transform.position, target.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, wantedRot, Time.deltaTime * chaseRotationDamping);
                }
                else
                    transform.LookAt(target, target.up);
            }
        }
    }

    void OnGUI() {
        if (inputEnabled && crosshair != null && dot != null) {
            float fract = Screen.height / 5;
            Vector2 aimPoint = new Vector2(aimScreenPoint.x, Screen.height - aimScreenPoint.y);
            GUI.DrawTexture(new Rect(Screen.width / 2 - 50, Screen.height / 2 - fract - 50, 100, 100), crosshair);
            GUI.DrawTexture(new Rect(aimPoint.x - 50, aimPoint.y - 50, 100, 100), dot);
        }
    }

    public void SetTarget(Transform target) {
        Vector3 angles = target.eulerAngles;
        x = angles.y;
        y = angles.x + 20;
        this.target = target;
        transform.position = target.parent.TransformPoint(0, chaseHeight, distance * -1);
        transform.rotation = Quaternion.LookRotation(target.position - transform.position, target.up);
        tankInst = target.GetComponent<TurretScript>();
        SetMode(freeCam);
    }

    public void SetMode(bool freeCam) {
        this.freeCam = freeCam;
        tankInst.SetEnabled(freeCam);
        transferingToFree = freeCam;
        transferT = 0;
    }

    public void SetInputEnable(bool enabled) {
        inputEnabled = enabled;
        mouseClickEnabled = enabled;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SetAimPoint(Vector3 point) {
        aimScreenPoint = point;
    }

    public static float ClampAngle(float angle, float min, float max) {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}