using UnityEngine;
using System.Collections;

public class SimpleTankControl : MonoBehaviour {
    public float m_Speed = 12f;
    public float m_TurnSpeed = 5f;

    private Rigidbody m_Rigidbody;

    // Use this for initialization
    void Start () {
        m_Rigidbody = GetComponent<Rigidbody>();
    }
	
	// Update is called once per frame
	void Update () {
        float mov = Input.GetAxis("Vertical");
        float rot = Input.GetAxis("Horizontal");

        if (mov < -0.01f)
            rot *= -1;

        float movement = mov * (m_Speed);
        Vector3 dir = m_Rigidbody.rotation * Vector3.forward;
        m_Rigidbody.velocity = new Vector3(dir.x * movement, m_Rigidbody.velocity.y, dir.z * movement);
        m_Rigidbody.angularVelocity = new Vector3(0, rot * m_TurnSpeed, 0);
        m_Rigidbody.rotation = Quaternion.Euler(0, m_Rigidbody.rotation.eulerAngles.y, 0);
    }

    void FixedUpdate() {

    }
}
