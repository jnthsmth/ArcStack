﻿using UnityEngine;
using System.Collections.Generic;

public enum TranslationMode {
	GlobalMotion,
	ThrusterOnly,
	Strafing
}

[RequireComponent(typeof(Controls))]
[RequireComponent(typeof(Rigidbody))]
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
	public float maxBoostTime = 2f;
	public float boostTime = 2f;
	public float boostFactor = 2f;
	public float boostRefillTime = 5f;

	private Rigidbody rigid;
	private Controls controls;
	private ParticleSystem particles;

	void Start() {
		rigid = GetComponent<Rigidbody>();
		controls = GetComponent<Controls>();
		particles = GetComponent<ParticleSystem>();
	}

	void Update() {
		if (controls.BoostButtonIsPressed) boostTime -= Time.deltaTime;
		else boostTime += Time.deltaTime * maxBoostTime / boostRefillTime;
		if (boostTime < 0f) boostTime = 0f;
		if (boostTime > maxBoostTime) boostTime = maxBoostTime;
	}

	void FixedUpdate() {

		Vector3 pos = transform.position;
		pos.y = 0f;
		transform.position = pos;

		rigid.velocity *= 1 - friction;

		float alignment = Vector3.Dot(transform.forward, rigid.velocity.normalized);
		if (!backwardPenalty && alignment < 0) alignment = 0f;
		float effectiveMaxSpeed = maxSpeed + alignment * alignmentEffect * maxSpeed;
		float effectiveAcceleration = acceleration;
		if (controls.BoostButtonIsPressed && boostTime > 0f) {
			effectiveMaxSpeed *= boostFactor;
			effectiveAcceleration *= boostFactor;
			ParticleSystem.EmissionModule em = particles.emission;
			em.enabled = true;
		} else {
			ParticleSystem.EmissionModule em = particles.emission;
			em.enabled = false;
		}

		Vector3 moveControl = new Vector3(controls.MoveStick.x, 0f, controls.MoveStick.y);
		moveControl *= Time.deltaTime * effectiveAcceleration;

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
