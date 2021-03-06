﻿using UnityEngine;
using System.Collections.Generic;

public enum TranslationMode {
	GlobalMotion,
	ThrusterOnly,
	Strafing
}

[RequireComponent(typeof(Controls))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Player))]
public class ShipControls : MonoBehaviour {

	[Header("Ship Parameters")]

	public TranslationMode shipTranslationMode = TranslationMode.GlobalMotion;

	public float minSpeed = 1f;
	public float maxSpeed = 7f;
	public float acceleration = 6f;
	public float friction = 0.01f;
	public float alignmentEffect = .2f;
	public float speedLimitLerpSpeed = .1f;
	public bool backwardPenalty = false;

	public float boostFactor = 2f;

	public float boostTime = 2f;
	public float boostTimeRemaining = 0f;

	public float boostCooldownTime = 3f;
	public float boostCooldownRemaining = 0f;

	public float remainingStunTime = 0f;

	private Rigidbody rigid;
	private Controls controls;
	private ParticleSystem particles;
	private ParticleSystem.EmissionModule emitter;
	private Player player;

	void Start() {
		rigid = GetComponent<Rigidbody>();
		controls = GetComponent<Controls>();
		particles = GetComponent<ParticleSystem>();
		player = GetComponent<Player> ();
		if (particles != null) emitter = particles.emission;
	}

	void Update() {


		if (remainingStunTime > 0f) remainingStunTime -= Time.deltaTime;
		if (remainingStunTime < 0f) remainingStunTime = 0f;

		if (player.currState == Player.State.Dead) {
			boostTimeRemaining = 0f;
			return;
		}
			
		boostCooldownRemaining -= Time.deltaTime;
		if (boostCooldownRemaining < 0f) boostCooldownRemaining = 0f;

		boostTimeRemaining -= Time.deltaTime;
		if (boostTimeRemaining < 0f) boostTimeRemaining = 0f;
	}

	void FixedUpdate() {

		Vector3 pos = transform.position;
		pos.y = 0f;
		transform.position = pos;

		float alignment = Vector3.Dot(transform.forward, rigid.velocity.normalized);
		if (!backwardPenalty && alignment < 0) alignment = 0f;
		float effectiveMaxSpeed = maxSpeed + alignment * alignmentEffect * maxSpeed;
		float effectiveAcceleration = acceleration;

        if (controls.BoostButtonWasPressed && boostCooldownRemaining <= 0f)
        {
            boostCooldownRemaining = boostCooldownTime;
			boostTimeRemaining = boostTime;
        }

        if (boostTimeRemaining > 0f){
			effectiveMaxSpeed *= boostFactor;
			effectiveAcceleration *= boostFactor;
			emitter.enabled = true;
		} else {
			emitter.enabled = false;
		}
	

		Vector3 moveControl = new Vector3(controls.MoveStick.x, 0f, controls.MoveStick.y);
		moveControl *= Time.deltaTime * effectiveAcceleration;

		if (remainingStunTime > 0f) {
			if (rigid.velocity.magnitude > effectiveMaxSpeed) rigid.velocity *= 1 - speedLimitLerpSpeed;
			if (rigid.velocity.magnitude <= minSpeed) rigid.velocity *= 1 + speedLimitLerpSpeed;
			return;
		}

		rigid.velocity *= 1 - friction;
		switch (shipTranslationMode) {
			case TranslationMode.GlobalMotion:
				rigid.velocity += moveControl;
				break;
			case TranslationMode.Strafing:
				rigid.velocity += transform.TransformDirection(moveControl);
				break;
			case TranslationMode.ThrusterOnly:
				float speedSign = Vector3.Dot(rigid.velocity, transform.forward) > 0 ? 1f : -1f;
				float forwardSpeed = speedSign * rigid.velocity.magnitude + moveControl.z;
				Vector3 localVelocity = new Vector3(0f, 0f, forwardSpeed);
				rigid.velocity = transform.TransformDirection(localVelocity);
				break;
			default:
				break;
		}
		
		if (rigid.velocity.magnitude > effectiveMaxSpeed) rigid.velocity *= 1 - speedLimitLerpSpeed;
		if (rigid.velocity.magnitude <= minSpeed) rigid.velocity *= 1 + speedLimitLerpSpeed;
	}
}
