using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sensor : MonoBehaviour
{

    private List<string> forbidden_collisions = new List<string> {"House", "Building", "Skyscraper", "Road"};

    void OnTriggerEnter(Collider other) {
        
        if (forbidden_collisions.Contains(other.gameObject.name)) {

            GameObject prefab = gameObject.transform.parent.gameObject;
            GameObject plane = gameObject.transform.parent.gameObject.transform.parent.gameObject;

            /* Remove from memory deleted buildings */
            plane.GetComponent<buildCity>().removeBuildingFromList(this.gameObject.name, prefab);

            /* Relocate inhabitants of destroyed non empty buildings */
            List<GameObject> inhabitants = prefab.GetComponent<Building>().inhabitants;

            for (int i = 0 ; i < inhabitants.Count ; i++) {
                plane.GetComponent<buildCity>().AddHomeless(inhabitants[i]);
                //Debug.Log("Added 1 homeless");
            }

            /* Relocate workers of destroyed non empty buildings */
            List<GameObject> workers = prefab.GetComponent<Building>().workers;

            for (int i = 0 ; i < workers.Count ; i++) {
                plane.GetComponent<buildCity>().AddWorkless(workers[i]);
                //Debug.Log("Added 1 workless");
            }
            
            Destroy(prefab);
        }
        
        
    }
}
