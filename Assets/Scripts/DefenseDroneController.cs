﻿using UnityEngine;
using System.Collections;

public class DefenseDroneController : AI {

	[Header("DefenseDrone: Inspector Set Fields")]
	public float spinSpeed;
	public float rotationSpeed;
	public float thrusterSpeed;
	public float drag;
	public GameObject weaponPrefab;
	public float delayBetweenShots = 0.5f;
	public float angleChange = 0.5f;
	public float angleOffset = 0.05f;

	public float radius = 3f;
	public float radiusOffset = 0.2f;

	float currThrusterSpeed;

	public LayerMask raycastMask;

	[Header("DefenseDrone: Dynamically Set Fields")]
	public GameObject CPSpawn;
	public GameObject enemyBase;
	public float currRadius;
	public float currAngle;

	public float elapsedFireDelay;
	public NavMeshAgent NMAgent;
	Transform mesh;
	Rigidbody RB;

	void Start () {

		if (teamNumber == Team.Team1) {
			gameObject.layer = LayerMask.NameToLayer("BlueDrone");
			foreach (Transform tr in transform) {
				if (tr.gameObject.name == "Range")
					tr.gameObject.layer = LayerMask.NameToLayer("BlueWeapon");
				else
					tr.gameObject.layer = LayerMask.NameToLayer("BlueDrone");
			}
		}
			
		if (teamNumber == Team.Team2) {
			gameObject.layer = LayerMask.NameToLayer("RedDrone");
			foreach (Transform tr in transform) {
				if (tr.gameObject.name == "Range")
					tr.gameObject.layer = LayerMask.NameToLayer("RedWeapon");
				else
					tr.gameObject.layer = LayerMask.NameToLayer("RedDrone");
			}
		}

		mesh = transform.Find ("Mesh1");
		elapsedFireDelay = 0f;
		RB = GetComponent<Rigidbody> ();

		NMAgent = GetComponent<NavMeshAgent> ();
		
		currRadius = radius;
		currAngle = 0f;

		circleCP ();
	}

	void Update () {

		spin ();

		// If there is something in range
		if (range.inRange.Count > 0) {
			NMAgent.Stop ();

			rotateToFaceEnemy (GetTarget(range.inRange));	
			thruster ();
			fireWeapon (GetTarget(range.inRange));
		}
		// Otherwise, go around and do stuff;
		else {
			circleCP ();
		}
	}

    protected override void DoOnDamage()
	{
		Instantiate (MusicMan.MM.damageSoundSource, transform.position, Quaternion.identity);
        Color tcolor = GameManager.GM.getTeamColor(teamNumber);
        ShipColor scolor = transform.Find("Mesh1").GetComponent<ShipColor>();
        if (scolor != null)
        {
            scolor.FlashColor(Color.Lerp(tcolor, Color.white, .75f), .1f);
        }
    }

    void spin() {
		mesh.Rotate (0f, spinSpeed * RB.velocity.magnitude, 0f);
	}

	void rotateToFaceEnemy(GameObject enemy) {
		if (enemy == null) return;
		Vector3 direction =
			enemy.transform.position - transform.position;

		currThrusterSpeed = direction.magnitude / thrusterSpeed;

		Quaternion targetRotation =
			Quaternion.LookRotation(direction, Vector3.up);

		transform.rotation = Quaternion.Slerp (transform.rotation, targetRotation, rotationSpeed);
	}

	void thruster() {
		RB.velocity = RB.velocity * (1 - drag);
		RB.AddForce (transform.forward * currThrusterSpeed);
	}

	void fireWeapon(GameObject enemy) {
		elapsedFireDelay += Time.deltaTime;
		if (elapsedFireDelay >= delayBetweenShots)
		{
			elapsedFireDelay = 0f;

			GameObject weapon = Instantiate(weaponPrefab);
			weapon.transform.position = transform.position;
			Instantiate (MusicMan.MM.droneBulletSoundSource, transform.position, Quaternion.identity);

			// make bullets tiny lol
			weapon.transform.localScale = new Vector3 (0.3f, 0.3f, 0.3f);

			if (teamNumber == Team.Team1) {
				weapon.layer = LayerMask.NameToLayer ("BlueWeapon");
			} else if (teamNumber == Team.Team2) {
				weapon.layer = LayerMask.NameToLayer ("RedWeapon");
			}

			Weapon weaponComp = weapon.GetComponent<Weapon>();

			weaponComp.startingVelocity = transform.forward;
			weaponComp.startingVelocity.Normalize();
			weaponComp.originator = this;

			weaponComp.damagePower = 0.5f;
		}
	}

	// Deal with clean up after death
	void OnDestroy() {
        if(CPSpawn != null)
        {
            if (teamNumber == Team.Team1)
            {
                CPSpawn.GetComponent<SpawnDrone>().spawnedDDrones_Team1.Remove(this.gameObject);
            }
            else if (teamNumber == Team.Team2)
            {
                CPSpawn.GetComponent<SpawnDrone>().spawnedDDrones_Team2.Remove(this.gameObject);
            }
        }
	}

	void circleCP() {
		NMAgent.Resume ();

		//Debug.DrawRay (NMAgent.destination, 10f*Vector3.up, Color.red, 1f);

		if (NMAgent.remainingDistance <= 0.1f) {

			//Debug.DrawRay (NMAgent.destination, 10f * Vector3.up, Color.cyan, 1f);

			Vector3 cpPos = CPSpawn.transform.position;
			float offsetX, offsetZ;
			float newAngle = currAngle + Random.Range (angleChange - angleOffset, angleChange + angleOffset);
			float newRadius = Random.Range (radius - radiusOffset, radius + radiusOffset);

			offsetX = newRadius * Mathf.Cos (newAngle);
			offsetZ = newRadius * Mathf.Sin (newAngle);

			Vector3 newPos = new Vector3 (cpPos.x + offsetX, transform.position.y, cpPos.z + offsetZ);

			NMAgent.SetDestination (newPos);

			currAngle = newAngle;
			currRadius = newRadius;
		} else {
			//Debug.DrawRay (transform.position, 10f * Vector3.up, Color.green, 1f);
			// Keep drones from getting stuck near base for some reason
			thruster ();
		}
	}
}
