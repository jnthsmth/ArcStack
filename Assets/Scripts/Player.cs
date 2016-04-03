﻿using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Controls))]
public class Player : HealthSystem
{
    public enum State { Normal = 0, Dead, size };

    [Header("Player: Status")]
    public State currState = State.Normal;

    [Header("Player Weapon Config")]
    public GameObject[] weapons;
    public int selectedWeapon = 0;

    public Transform[] turretTransforms;
    private Controls controls;

    public float rateOfFire = 10f;
    private float timeSinceShot = 0f;

    [Header("Player Respawn Config")]
    public Transform respawnLocation;
    public float respawnDelayTime = 3f;
    public float maxRandomOffset = .5f;
    private float currDelayTime;
    private Vector3 respawnLocationVector;

    protected override void OnAwake()
    {
        List<Transform> turrets = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.name == "Turret")
            {
                Transform barrel = child.Find("Barrel");
                if (barrel == null)
                {
                    print("Turret has no barrel :(");
                    return;
                }
                turrets.Add(barrel);
            }
        }
        turretTransforms = turrets.ToArray();

        controls = GetComponent<Controls>();

        currDelayTime = respawnDelayTime;
        respawnLocationVector = respawnLocation.position;
        currState = State.Normal;
    }

    protected override void DoOnUpdate()
    {
        timeSinceShot += Time.deltaTime;

        switch (currState)
        {
            case State.Normal:
                if (controls.FireButtonIsPressed && weapons.Length > selectedWeapon)
                {
                    Fire(weapons[selectedWeapon]);
                }
                break;

            case State.Dead:
                ManageDeadState();
                break;

            default:
                break;
        }
    }

    protected override void DoOnFixedUpdate()
    {
        if (tookDamage)
        {
            controls.VibrateFor(.25f, .1f);
        }
    }

    public override void DeathProcedure()
    {
        if (currState != State.Dead)
        {
            currState = State.Dead;
            controls.VibrateFor(.5f, .5f);
        }
    }

    void ManageDeadState()
    {
        if (currDelayTime > 0)
        {
            currDelayTime -= Time.deltaTime;

            tookDamage = false;
            beingHit = false;

            transform.Find("Mesh1").GetComponent<MeshRenderer>().enabled = false;
            transform.Find("Mesh1").GetComponent<Collider>().enabled = false;
            transform.Find("Turret/Barrel").GetComponent<MeshRenderer>().enabled = false;
            transform.Find("Turret/Barrel").GetComponent<Collider>().enabled = false;
            transform.Find("HealthBar(Clone)").gameObject.SetActive(false);
        }
        else
        {
            transform.position = new Vector3(respawnLocationVector.x
                                             + Random.Range(-maxRandomOffset, maxRandomOffset),
                                             transform.position.y,
                                             respawnLocationVector.z
                                             + Random.Range(-maxRandomOffset, maxRandomOffset));
            //renable the player
            currState = State.Normal;
            currHealth = maxHealth;
            transform.Find("Mesh1").GetComponent<MeshRenderer>().enabled = true;
            transform.Find("Mesh1").GetComponent<Collider>().enabled = true;
            transform.Find("Turret/Barrel").GetComponent<MeshRenderer>().enabled = true;
            transform.Find("Turret/Barrel").GetComponent<Collider>().enabled = true;
            transform.Find("HealthBar(Clone)").gameObject.SetActive(true);
            currDelayTime = respawnDelayTime;
        }
    }

    void Fire(GameObject weapon)
    {

        if (timeSinceShot < 1f / rateOfFire) return;

        foreach (Transform turret in turretTransforms)
        {
            GameObject go = Instantiate(weapon) as GameObject;
            Weapon weaponScript = go.GetComponent<Weapon>();
            weaponScript.teamNumber = teamNumber;
            weaponScript.startingVelocity = turret.forward;
            weaponScript.maxDistance = 50f;
            go.transform.position = turret.position;
        }
        timeSinceShot = 0f;
    }
}