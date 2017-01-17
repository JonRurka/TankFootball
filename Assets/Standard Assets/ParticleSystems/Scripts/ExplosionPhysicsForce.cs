using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Effects;

public class ExplosionPhysicsForce : MonoBehaviour
{
    public float explosionForce = 4;

    public bool IsClient = true;


    private IEnumerator Start()
    {
        // wait one frame because some explosions instantiate debris which should then
        // be pushed by physics force
        yield return null;

        if (!IsClient) {
            float multiplier = GetComponent<ParticleSystemMultiplier>().multiplier;

            float r = 10 * multiplier;
            var cols = Physics.OverlapSphere(transform.position, r);
            var rigidbodies = new List<Rigidbody>();
            foreach (var col in cols) {
                if (col.attachedRigidbody != null && !rigidbodies.Contains(col.attachedRigidbody)) {
                    rigidbodies.Add(col.attachedRigidbody);
                    /*if (col.transform.root.tag == "enemy") {
                        col.transform.root.SendMessage("BlowUp", new Vector4(
                            transform.position.x, transform.position.y, transform.position.z, explosionForce * multiplier));
                    }*/
                }
            }
            foreach (var rb in rigidbodies) {
                if (rb != null) {
                    rb.AddExplosionForce(explosionForce * multiplier, transform.position, r, 1 * multiplier, ForceMode.Impulse);
                }
            }
        }
    }
}

