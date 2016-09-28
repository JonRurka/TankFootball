using UnityEngine;
using System;

public class TankMovement : MonoBehaviour {
    public float m_Speed = 12f;                 // How fast the tank moves forward and back.
    public float m_speedAdjust = 0;
    public float m_maxAdjust = 50;
    public float m_maxSpeed = 0;
    public float m_TurnSpeed = 180f;            // How fast the tank turns in degrees per second.
    public float acceloration = 2;
    public AudioSource m_MovementAudio;         // Reference to the audio source used to play engine sounds. NB: different to the shooting audio source.
    public AudioClip m_EngineIdling;            // Audio to play when the tank isn't moving.
    public AudioClip m_EngineDriving;           // Audio to play when the tank is moving.
    public float m_PitchRange = 0.2f;           // The amount by which the pitch of the engine noises can vary.
    public float speedChangeCooldown;
    public float speedChangeMax;
    public float MPH = 0;
    public float Vel = 0;
    public bool isOwner = false;
    public byte ID;
    public Vector2 turretRotation;
    public GameObject BallClamp;
    public TurretScript turrentInst;
    public bool HasBall;
    public bool Enabled = true;

    public Renderer leftTrackRend;
    public Renderer rightTrackRend;
    private float leftTexOffset = 0;
    private float rightTexOffset = 0;
    public float trackSpeed = 1;

    public Rigidbody m_Rigidbody;              // Reference used to move the tank.
    private float m_MovementInputValue;         // The current value of the movement input.
    private float m_TurnInputValue;             // The current value of the turn input.
    private float m_OriginalPitch;              // The pitch of the audio source at the start of the scene.
    public float vertDelta;
    public float horzDelta;
    public byte verState;
    public byte horzState;
    private float lastSyncTime = 0f;
    private float syncDelay = 0f;
    private float syncTime = 0f;
    private Vector3 syncStartPosition;
    private Vector3 syncEndPosition;
    private Quaternion syncStartRotation;
    private Quaternion syncEndRotation;
    public float sound = 0;

    private Vector3 targetRotation;

    private void Awake() {
        m_Rigidbody = GetComponent<Rigidbody>();
        turrentInst = GetComponentInChildren<TurretScript>();
    }

    private void Start() {
        m_speedAdjust = UnityEngine.Random.Range(-m_maxAdjust, m_maxAdjust + 1);
        m_speedAdjust = 0;
        m_maxSpeed = m_Speed + m_speedAdjust;
        m_OriginalPitch = m_MovementAudio.pitch;
        if (isOwner) {
            MouseOrbitImproved.Instance.SetTarget(turrentInst.transform);
        }
        syncStartRotation = m_Rigidbody.rotation;
        syncEndRotation = m_Rigidbody.rotation;
    }

    private void Update() {
        turretRotation = turrentInst.turretRotation;
        if (isOwner) {
            m_MovementInputValue = Input.GetAxis("Vertical");
            m_TurnInputValue = Input.GetAxis("Horizontal");

            byte vert = 0;
            byte horz = 0;
            if (m_MovementInputValue > 0.01f)
                vert = 1;
            else if (m_MovementInputValue < -0.01f)
                vert = 2;

            if (m_TurnInputValue > 0.01f)
                horz = 1;
            else if (m_TurnInputValue < -0.01f)
                horz = 2;

            Int16 sx = PlayerControl.ToShort(PlayerControl.Repeat(turretRotation.x, 360), 90);
            Int16 sy = PlayerControl.ToShort(PlayerControl.Repeat(turretRotation.y, 360), 90);
            byte[] bx = PlayerControl.ToByte(sx);
            byte[] by = PlayerControl.ToByte(sy);
            byte[] sendBytes = new byte[] { vert, horz, bx[0], bx[1], by[0], by[1]  };
            NetClient.Instance.Send(NetServer.OpCodes.SubmitInput, sendBytes );
        }

        if (GameControl.Instance.IsServer) {
            SetMovementDeltas();
        }
        else {
            syncTime += Time.deltaTime;
            m_Rigidbody.position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
            m_Rigidbody.rotation = Quaternion.Lerp(syncStartRotation, syncEndRotation, syncTime / syncDelay);
        }

        EngineAudio();
    }

    // only called on server.
    private void SetMovementDeltas() {
        // STOP
        if (!Enabled) {
            verState = 0;
            horzState = 0;
        }

        if (verState == 0 && vertDelta > 0) {
            vertDelta -= Time.deltaTime * acceloration;
        }
        if (verState == 0 && vertDelta < 0) {
            vertDelta += Time.deltaTime * acceloration;
        }

        if (vertDelta < 0.01f && vertDelta > -0.01f && verState == 0) {
            vertDelta = 0;
        }

        // FORWARD
        if (verState == 1) {
            vertDelta += Time.deltaTime * acceloration;
        }

        // BACKWARDS
        if (verState == 2) {
            vertDelta -= Time.deltaTime * acceloration;
        }

        // STOP TURN
        if (horzState == 0 && horzDelta > 0) {
            horzDelta -= Time.deltaTime * acceloration;
        }
        if (horzState == 0 && horzDelta < 0) {
            horzDelta += Time.deltaTime * acceloration;
        }

        // LEFT
        int reverseModifier = 1;
        if (verState == 2)
            reverseModifier = -1;

        if (horzState == 1) {
            horzDelta += Time.deltaTime * acceloration * reverseModifier;
        }

        // RIGHT
        if (horzState == 2) {
            horzDelta -= Time.deltaTime * acceloration * reverseModifier;
        }

        vertDelta = Mathf.Clamp(vertDelta, -1, 1);
        horzDelta = Mathf.Clamp(horzDelta, -1, 1);
        sound = Mathf.Abs(vertDelta);

    }

    private void TrackAnimation() {
        float mov = Mathf.Clamp(vertDelta, -1, 1);
        float rot = Mathf.Clamp(horzDelta, -1, 1);

        if (mov < 0) {
            rot *= -1;
            // backwards tracks
            leftTexOffset += trackSpeed * mov * Time.deltaTime;
            rightTexOffset += trackSpeed * mov * Time.deltaTime;

            leftTrackRend.material.SetTextureOffset("_MainTex", new Vector2(leftTexOffset, 0));
            leftTrackRend.material.SetTextureOffset("_BumpMap", new Vector2(leftTexOffset, 0));

            rightTrackRend.material.SetTextureOffset("_MainTex", new Vector2(rightTexOffset, 0));
            rightTrackRend.material.SetTextureOffset("_BumpMap", new Vector2(rightTexOffset, 0));
        }
        else if (mov > 0) {
            // forwards tracks
            leftTexOffset += trackSpeed * mov * Time.deltaTime;
            rightTexOffset += trackSpeed * mov * Time.deltaTime;

            leftTrackRend.material.SetTextureOffset("_MainTex", new Vector2(leftTexOffset, 0));
            leftTrackRend.material.SetTextureOffset("_BumpMap", new Vector2(leftTexOffset, 0));

            rightTrackRend.material.SetTextureOffset("_MainTex", new Vector2(rightTexOffset, 0));
            rightTrackRend.material.SetTextureOffset("_BumpMap", new Vector2(rightTexOffset, 0));
        }
        else if (rot < 0) {
            // left turn tracks
            leftTexOffset += trackSpeed * rot * Time.deltaTime;
            rightTexOffset -= trackSpeed * rot * Time.deltaTime;

            leftTrackRend.material.SetTextureOffset("_MainTex", new Vector2(leftTexOffset, 0));
            leftTrackRend.material.SetTextureOffset("_BumpMap", new Vector2(leftTexOffset, 0));

            rightTrackRend.material.SetTextureOffset("_MainTex", new Vector2(rightTexOffset, 0));
            rightTrackRend.material.SetTextureOffset("_BumpMap", new Vector2(rightTexOffset, 0));
        }
        else if (rot > 0) {
            // right turn tracks
            leftTexOffset -= trackSpeed * rot * Time.deltaTime;
            rightTexOffset += trackSpeed * rot * Time.deltaTime;

            leftTrackRend.material.SetTextureOffset("_MainTex", new Vector2(leftTexOffset, 0));
            leftTrackRend.material.SetTextureOffset("_BumpMap", new Vector2(leftTexOffset, 0));

            rightTrackRend.material.SetTextureOffset("_MainTex", new Vector2(rightTexOffset, 0));
            rightTrackRend.material.SetTextureOffset("_BumpMap", new Vector2(rightTexOffset, 0));
        }

        if (leftTexOffset >= 10)
            leftTexOffset = 0;
        if (rightTexOffset >= 10)
            rightTexOffset = 0;
    }

    private void FixedUpdate() {
        if (GameControl.Instance.IsServer) {
            float mov = Mathf.Clamp(vertDelta, -1, 1);
            float rot = Mathf.Clamp(horzDelta, -1, 1);

            float movement = mov * (m_Speed + m_speedAdjust) * Time.deltaTime;
            m_Rigidbody.velocity = transform.TransformDirection(new Vector3(0, m_Rigidbody.velocity.y, movement));
            m_Rigidbody.angularVelocity = new Vector3(0, rot * m_TurnSpeed * Time.deltaTime, 0);
            m_Rigidbody.rotation = Quaternion.Euler(0, m_Rigidbody.rotation.eulerAngles.y, 0);
            MPH = new Vector2(m_Rigidbody.velocity.x, m_Rigidbody.velocity.z).magnitude * 2.237f;
            Vel = new Vector2(m_Rigidbody.velocity.x, m_Rigidbody.velocity.z).magnitude;
        }
    }

    private void OnEnable() {
        m_Rigidbody.isKinematic = false;

        m_MovementInputValue = 0f;
        m_TurnInputValue = 0f;
    }

    private void OnDisable() {
        m_Rigidbody.isKinematic = true;
    }

    private void EngineAudio() {
        // If there is no input (the tank is stationary)...
        float pitchRange = UnityEngine.Random.Range(-0.07f * sound, 0.07f * sound);
        m_MovementAudio.pitch = m_OriginalPitch + pitchRange + Mathf.Abs(sound) * 5/10;
        return;
        /*if (Mathf.Abs(m_MovementInputValue) < 0.1f && Mathf.Abs(m_TurnInputValue) < 0.1f) {
            // ... and if the audio source is currently playing the driving clip...
            if (m_MovementAudio.clip == m_EngineDriving) {
                // ... change the clip to idling and play it.
                m_MovementAudio.clip = m_EngineIdling;
                //m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play();
            }
        }
        else {
            // Otherwise if the tank is moving and if the idling clip is currently playing...
            if (m_MovementAudio.clip == m_EngineIdling) {
                // ... change the clip to driving and play.
                m_MovementAudio.clip = m_EngineDriving;
                //m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play();
            }
        }*/
    }

    public void SetPosition(Vector3 position) {
        m_Rigidbody.position = position;
    }

    public void SetRotation(Vector3 rotation) {
        m_Rigidbody.rotation = Quaternion.Euler(rotation);
    }

    public void SetEnabled(bool enabled) {
        Enabled = enabled;
        turrentInst.SetEnabled(enabled);
        if (GameControl.Instance.IsServer)
            NetServer.Instance.Send(ID, NetClient.OpCodes.SetEnabled, new byte[] { ID, BitConverter.GetBytes(enabled)[0] });
    }

    // only called on server.
    public void SetInput(byte vert, byte horiz, Vector2 turrent) {
        if (GameControl.Instance.IsServer) {
            verState = vert;
            horzState = horiz;
            turretRotation = turrent;
            if (!isOwner)
                turrentInst.SetTurretRotation(turrent);
        }
    }

    // only called on server
    public PlayerControl.MovementData GetPlayerData() {
        return new PlayerControl.MovementData(ID,
                                               new Vector2(m_Rigidbody.position.x, m_Rigidbody.position.z),
                                               new Vector2(m_Rigidbody.velocity.x, m_Rigidbody.velocity.z),
                                               m_Rigidbody.rotation.eulerAngles.y, turrentInst.turretRotation, sound);
    }

    // only called on client.
    public void SetPlayerData(PlayerControl.MovementData data) {
        if (!GameControl.Instance.IsServer) {
            syncTime = 0f;
            syncDelay = Time.time - lastSyncTime;
            lastSyncTime = Time.time;

            syncStartPosition = m_Rigidbody.position;
            syncEndPosition = new Vector3(data.Location.x, 0, data.Location.y)/* + new Vector3(data.velocity.x, 0, data.velocity.y) * syncDelay*/;

            Vel = Vector3.Distance(syncStartPosition, syncEndPosition) / syncDelay;

            syncStartRotation = m_Rigidbody.rotation;
            syncEndRotation = Quaternion.Euler(new Vector3(0, data.Rotation, 0));

            turrentInst.SetTurretRotation(data.TurretRotation);

            //sound = data.Sound;
        }
    }
}
