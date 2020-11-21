using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sensor : MonoBehaviour
{

    private List<string> forbidden_collisions = new List<string> {"House", "Building", "Skyscraper", "Road"};

    void OnTriggerEnter(Collider other) {
        
        if (forbidden_collisions.Contains(other.gameObject.name)) {
            //Debug.Log(gameObject.name + " got triggered by " + other.gameObject.name);
            Destroy(gameObject.transform.parent.gameObject);
        }
        
        
    }
}
