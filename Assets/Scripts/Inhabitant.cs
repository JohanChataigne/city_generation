using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

public class Inhabitant : MonoBehaviour
{
    private GameObject home;
    private GameObject workplace;
    public NavMeshAgent agent;

    /* Distance threshold to consider agent as arrived to his destination */
    private float threshold = 0.3f;

    [SerializeField]
    private GameObject current_destination;
    [SerializeField]
    private bool is_moving = false;

    /* Custom important times for this agent */
    [SerializeField]
    private float start_time;
    [SerializeField]
    private float end_time;

    /* Current activity of the agent */
    [SerializeField]
    private bool working = false;
    [SerializeField]
    private bool chilling = true;

    public void Init(GameObject home, float start, float end) {
        this.SetHome(home);
        this.start_time = start;
        this.end_time = end;
        Debug.Log("Finish init");
    }

    public GameObject GetHome() {
        return this.home;
    }

    public void SetHome(GameObject home) {
        this.home = home;
    }

    public void GetOut(GameObject building) {

        /* In case of 1st time leaving */
        agent.isStopped = false;

        /* Switch off a light of the building */
        building.GetComponent<Building>().OnAgentLeave();

        /* Move and activate inhabitant */
        this.gameObject.GetComponent<MeshRenderer>().enabled = true;

        Debug.Log("Finish getout");
        
    }

    public void GetIn(GameObject building) {

        /* Stop agent and place him in the building */
        agent.isStopped = true;
        this.is_moving = false;
        this.gameObject.GetComponent<MeshRenderer>().enabled = false;

        /* Switch on a light */
        building.GetComponent<Building>().OnAgentEnter();

        /* Switch activity of the agent */
        if (building == this.workplace) this.working = true;
        else if (building == this.home) this.chilling = true;
    }

    public void SetWorkPlace(GameObject workplace) {
        this.workplace = workplace;
    }

    public void GoToWork() {
        
        /* Stop chilling */
        this.chilling = false;

        /* Get out of home and prepare to move */
        GetOut(this.home);

        /* Set destination to work */
        Vector3 work_position = workplace.transform.position;
        agent.SetDestination(new Vector3(work_position.x, 0, work_position.z));
        this.is_moving = true;
        current_destination = workplace;

    }

    public void GoHome() {

        /* Stop working */
        this.working = false;

         /* Get out of work and prepare to move */
        GetOut(this.workplace);

        /* Set destination to home */
        Vector3 home_position = home.transform.position;
        agent.SetDestination(new Vector3(home_position.x, 0, home_position.z));
        this.is_moving = true;
        current_destination = home;
    }

    private bool HasArrived() {
        float dist = Vector3.Distance(this.transform.position, agent.destination);
        return (dist < threshold);
    }

    /* Get current time from day night controller */
	private float GetCurrentTime() { return this.transform.parent.GetComponent<DayNightController>().currentTimeOfDay; }

    void Update() {

        /* If inhabitant moving to a location, make him enter if he has arrived */
        if (this.is_moving) {
            if (HasArrived()) {
                GetIn(current_destination);
            }
        }

        else {

            /* Handle actions start timing */
            if (GetCurrentTime() >= this.end_time && this.working) {
                GoHome();
            }

            else if (GetCurrentTime() >= this.start_time && GetCurrentTime() <= this.end_time && this.chilling) {
                GoToWork();
            }

        }
        
    }
}

