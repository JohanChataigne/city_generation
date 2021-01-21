using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Building : MonoBehaviour
{
    [SerializeField]
    private int capacity;

    public List<GameObject> inhabitants = new List<GameObject>();
    public List<GameObject> workers = new List<GameObject>();

    public GameObject window;
    private Material window_material;

    private Vector3 window_dim = new Vector3(0.01f, 0.05f, 0.05f);

    private List<GameObject> m_windows = new List<GameObject>();

    [SerializeField]
    private bool is_habitation = false;
    
    /* Direction vector to from the building to the road */
    private Vector3 toRoad;
    
    public void Init(Vector3 toRoad)
    {

        this.toRoad = toRoad;
        float height = this.transform.localScale.y;
        float width = this.transform.localScale.x;

        /* Compute ng inhabitants according to the building height */
        capacity = (int) (height / width);
        
        /* Instanciate as many windows as needed */
        for (int i = 0; i < capacity; i++) {
            
            Vector3 angles = this.transform.rotation.eulerAngles;
            Vector3 building_position = this.transform.position;

            float x_offset = width * (float)Math.Cos(-angles.y * Math.PI / 180);
            float z_offset = width * (float)Math.Sin(-angles.y * Math.PI / 180);

            /* Compute window position */
            Vector3 window_position = new Vector3(building_position.x + x_offset, i * width + width/2, building_position.z + z_offset);

            /* Create window and set properties */
            GameObject go_window = Instantiate(window, window_position, Quaternion.Euler(0, angles.y, 0));
            go_window.transform.localScale = window_dim;
            go_window.transform.SetParent(this.transform, true);

            /* Set emission color */
            window_material = go_window.gameObject.GetComponent<Renderer>().material;
            window_material.SetColor("_EmissionColor", Color.yellow);

            /* Add windows to the windows list */
            m_windows.Add(go_window);

        }
    }

    public void OnAgentEnter() {

        for (int i = 0 ; i < m_windows.Count ; i++) {

            window_material = m_windows[i].gameObject.GetComponent<Renderer>().material;
            if (!window_material.IsKeywordEnabled("_EMISSION")) {
                window_material.EnableKeyword("_EMISSION");
                break;
            }
        }

    }

    public void OnAgentLeave() {

        for (int i = m_windows.Count - 1 ; i >= 0 ; i--) {

            window_material = m_windows[i].gameObject.GetComponent<Renderer>().material;
            if (window_material.IsKeywordEnabled("_EMISSION")) {
                window_material.DisableKeyword("_EMISSION");
                break;
            }
        }

    }

    /* Simply adds an agent as inhabitant of the building */
    public void AddInhabitant(GameObject inhabitant) { 
    
        Debug.Assert(inhabitants.Count < capacity);

        inhabitants.Add(inhabitant);
        OnAgentEnter();
        
    }

    /* Simply adds an agent as worker of the building */
    public void AddWorker(GameObject worker) { 
    
        Debug.Assert(workers.Count < capacity);

        workers.Add(worker);
    }
    
    /* Wrapper function to set and get habitation property of a building */
    public void SwitchToWorkplace() { is_habitation = false; }
    public void SwitchToHabitation() { is_habitation = true; }
    public bool IsHabitable() { return is_habitation; }

    /* capacity getter */
    public int GetCapacity() { return capacity; }

    /* getter for road direction vector */
    public Vector3 GetRoadDirection() { return toRoad; }

    /* Checker for building current full state */
    public bool IsFull() { return inhabitants.Count == capacity || workers.Count == capacity; }

    /* Checks if building's empty */
    public bool IsEmpty() { return inhabitants.Count == 0 && workers.Count == 0; }

    // Update is called once per frame
    void Update()
    {
        
    }
}
