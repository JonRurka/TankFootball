using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour {
    public GameObject enemy;
    

	// Use this for initialization
	void Start () {
        InvokeRepeating("Spawn", 1, 5f);
	}
	
	// Update is called once per frame
	void Update () {
	    
	}

    public void Spawn() {
        int x = Random.Range(-60, 60);
        int z = Random.Range(-25, 25);
        Instantiate(enemy, new Vector3(x, 0, z), Quaternion.identity);
    }
}
