﻿using UnityEngine;
using System.Collections;

public class MoveFrame {

	public BezierCurve motion;
	public Vector3 startPosition;
	public Vector3 endPosition {
		get {
			return startPosition + motion.getPoint(1f);
		}
	}
	
}