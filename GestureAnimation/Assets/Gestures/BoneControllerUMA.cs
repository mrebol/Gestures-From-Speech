using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class BoneControllerUMA : BoneControllerBase
{
	private bool haveAnimator = false;
	private bool lipSyncDataExists = false;

	// Start is called before the first frame update
	protected override void Start()
	{
		//print("Start.");
		Application.runInBackground = true;

		switch (selectedInterval)
		{
			case Interval.Gruber1:
				speaker = "Gruber";
				interval = "3821";
				break;
			case Interval.Gruber2:
				speaker = "Gruber";
				interval = "1845";
				break;
			case Interval.Gruber3:
				speaker = "Gruber";
				interval = "3838";
				break;
			case Interval.Gruber4:
				speaker = "Gruber";
				interval = "3852";
				break;
			case Interval.Oliver1:
				speaker = "Oliver";
				interval = "102571";
				break;
		}
		
		if (speaker == "Oliver")
		{
			GameObject camera = GameObject.Find("Main Camera");
			camera.transform.rotation = Quaternion.Euler(-20,0,0) * camera.transform.rotation;
			camera.transform.position = new Vector3(0, 1.7f, 1);
		}
	}

	protected override void loadAudio()
	{
		string path = speaker + Path.DirectorySeparatorChar + interval;
		audioClip = Resources.Load<AudioClip>(path);
		audioData = GetComponent<AudioSource>();
		audioData.playOnAwake = false;
		audioData.clip = audioClip;
		print("Audio clip length is " + audioData.clip.length + "s.");
	}

	// Update is called once per frame
	protected override void Update()
	{
		if (!haveAnimator)
		{
			animator.runtimeAnimatorController = null; // Remove Locomotion Controller.
			if (animator.avatar == null)
				return;  // Avatar not loaded yet.

			GetBones();
			loadKeypoints();
			PointUpdate();
			if (speaker == "Gruber")
			{
				loadAudio();
				// Difference between keypoints and audio must be less then 0.5sec:
				Debug.Assert(Math.Abs(audioData.clip.length - (keypoints.Count / FrameRate)) < 0.5); 
			}

			
			SetBoneRot();
			if (speaker == "Gruber")
			{
				audioData.Play();
			}

			startTime = Time.time;
			haveAnimator = true;
		}
		else
		{
			if (speaker == "Gruber")
			{
				PointUpdateByAudioTime();
			}
			else
			{
				PointUpdateByStartTime();
			}

			SetBoneRot();
		}
	}

	protected override void SetBoneRot()
	{
		float interpolationFactor = 0.1f;
		float lowerArmRollRotationFactor = 0.0f;
		float handInterpolationFactor = 0.0f;
		if (speaker == "Gruber")
		{
			lowerArmRollRotationFactor = 0.3f;
			handInterpolationFactor = 0.02f;
				
		} else if (speaker == "Oliver")
		{
			lowerArmRollRotationFactor = 1.0f;
			handInterpolationFactor = 0.1f;
		}

		Vector3 shoulderLine = (points[1] - points[4]).normalized;
		Vector3 shoulderPlane = new Vector3(0,0,1);  //BoneList["Hips"].forward;//
		float shoulderRotation = (float) (Mathf.Rad2Deg * Math.Asin(Vector3.Dot(shoulderLine, shoulderPlane)) / (shoulderLine.magnitude * shoulderPlane.magnitude));

		Vector3 rightHandNeutralPlane = BoneList["RightHand"].forward;
		Vector3 rightHandNormalPlane = BoneList["RightHand"].right;
		
		Vector3 rightIndex2Little = (points[48] - points[36]).normalized; // TODO interpolate between fingers
		float rightHandRotationDelta = (float) (Mathf.Rad2Deg * Math.Asin(Vector3.Dot(rightIndex2Little, rightHandNeutralPlane)) / (rightIndex2Little.magnitude * rightHandNeutralPlane.magnitude));
		Vector3 rightLowerArmPlane = BoneList["RightLowerArm"].up;
		float rightLowerArmRotation = (float) (Mathf.Rad2Deg * Math.Asin(Vector3.Dot(rightIndex2Little, rightLowerArmPlane)) / (rightIndex2Little.magnitude * rightLowerArmPlane.magnitude));
		Quaternion rightThumbInitialRotation = Quaternion.Euler(-25, 30, -60); // x..in/out   y..left/right roll   z..towards/away middle finger
		Vector3 rightThumbPlane = (BoneList["RightHand"].rotation * rightThumbInitialRotation) * new Vector3(0, 0, 1);
		Vector3 rightThumbNormalPlane = (BoneList["RightHand"].rotation * rightThumbInitialRotation) * new Vector3(1, 0, 0);
		
	
		Vector3 leftHandNeutralPlane = BoneList["LeftHand"].forward;
		Vector3 leftHandBellguardPlane = BoneList["LeftHand"].right;
		Vector3 leftHandFlatPlane = BoneList["LeftHand"].up;
		
		Vector3 leftIndex2Little = (points[27] - points[15]).normalized; // Poss: interpolate between fingers
		float leftHandRotationDelta = (float) (Mathf.Rad2Deg * Math.Asin(Vector3.Dot(leftIndex2Little, leftHandNeutralPlane)) / (leftIndex2Little.magnitude * leftHandNeutralPlane.magnitude));
		float leftHandRotationOrientation = (float) (Mathf.Rad2Deg * Math.Asin(Vector3.Dot(leftIndex2Little, leftHandFlatPlane)) / (leftIndex2Little.magnitude * leftHandFlatPlane.magnitude));
		Quaternion leftThumbInitialRotation = Quaternion.Euler(+0, 40, +70);  // stretch, roll, towardsIndex
		Vector3 leftThumbPlane = (BoneList["LeftHand"].rotation * leftThumbInitialRotation) * new Vector3(0, 0, 1);
		Vector3 leftThumbNormalPlane = (BoneList["LeftHand"].rotation * leftThumbInitialRotation) * new Vector3(1, 0, 0);
		
		foreach (string bone in inputBones)
		{
			if (bonesForward.ContainsKey(bone) || bone.Contains("Distal")|| bone.Contains("Hips") || bone.Contains("Spine"))
			{
				Quaternion rotation1 = Quaternion.FromToRotation(new Vector3(0, 0, 0), new Vector3(0, 0, 0));
				Quaternion rotation2 = Quaternion.FromToRotation(new Vector3(0, 0, 0), new Vector3(0, 0, 0));
				Quaternion rotation3 = Quaternion.FromToRotation(new Vector3(0, 0, 0), new Vector3(0, 0, 0));
				Quaternion rotation4 = Quaternion.FromToRotation(new Vector3(0, 0, 0), new Vector3(0, 0, 0));

				if (bone == "Hips" && speaker == "Gruber")
				{
					//BoneList[bone].rotation = Quaternion.Lerp(BoneList[bone].rotation,Quaternion.Euler(0, -shoulderRotation, -90), interpolationFactor);
					continue;
				} else if (bone == "Spine" && speaker == "Oliver")  // LowerBack
				{
					BoneList[bone].rotation = Quaternion.Euler(10, 0, -90);
					BoneList["Neck"].rotation = Quaternion.Euler(-0, 0, -90);

					BoneList["Hips"].rotation = Quaternion.Euler(-10, 0, -90);
					BoneList["LeftUpperLeg"].rotation = Quaternion.Euler(-70, 0, 90);
					BoneList["RightUpperLeg"].rotation = Quaternion.Euler(-70, 0, 90);
					BoneList["LeftLowerLeg"].rotation = Quaternion.Euler(-0, 0, 90);
					BoneList["RightLowerLeg"].rotation = Quaternion.Euler(-0, 0, 90);
					continue;
				} 
				else if (bone == "RightUpperArm" )
				{
					rotation1 = Quaternion.FromToRotation(new Vector3(1, 0, 0), new Vector3(-1, 1, 0));
					rotation2 = Quaternion.FromToRotation(new Vector3(-1, 1, 0), -NormalizeBone[bone]);
					rotation3 = Quaternion.EulerAngles(new Vector3(0,50,0)); // ellbows closer to body correction
					//BoneList[bone].rotation = Quaternion.Lerp(BoneList[bone].rotation,	rotation2 * rotation1, 0.1f);
					BoneList[bone].rotation = Quaternion.Lerp(BoneList[bone].rotation,	rotation3 * rotation2 * rotation1, 0.1f);
					continue;
				}
				else if (bone == "RightLowerArm" )
				{
					rotation1 = Quaternion.FromToRotation(new Vector3(-1, 0, 0), new Vector3(0, 0, 1));
					rotation2 = Quaternion.EulerAngles(new Vector3(0,0,90)); // -30...go into neutral rotation
					rotation3 = Quaternion.FromToRotation(new Vector3(0, 0, 1), NormalizeBone[bone]);
					Vector3 rotation3Euler = rotation3.ToEulerAngles() * Mathf.Rad2Deg;
					rotation3 =  Quaternion.Euler(rotation3Euler.x, rotation3Euler.y, 0);
					rotation4 =  Quaternion.AngleAxis(+90-rightLowerArmRotation*lowerArmRollRotationFactor, NormalizeBone[bone]); //*3 because of Lerp and delta rotation
					BoneList[bone].rotation = Quaternion.Lerp(BoneList[bone].rotation,	rotation4 * rotation3 * rotation1, 0.1f);
					boneAngle[bone] = rightLowerArmRotation;
					continue;
				}
				else if (bone == "RightHand")
				{
					rotation1 = Quaternion.FromToRotation(new Vector3(-1, 0, 0), new Vector3(0, 0, 1));
					rotation2 = Quaternion.Euler(0, 0, 0); // intitial rotation
					rotation3 = Quaternion.FromToRotation(new Vector3(0, 0, 1), NormalizeBone[bone]);
					Vector3 rotation3Euler = rotation3.ToEulerAngles() * Mathf.Rad2Deg;
					rotation3 =  Quaternion.Euler(rotation3Euler.x, rotation3Euler.y, 0);
					rotation4 = Quaternion.AngleAxis(+rightHandRotationDelta, NormalizeBone[bone]);
					BoneList[bone].rotation = Quaternion.Lerp(BoneList[bone].rotation,rotation4* rotation3 * rotation2*rotation1, handInterpolationFactor);
					continue;
					
					
				}
				else if ((bone.Contains("Right") && (bone.Contains("Proximal") || bone.Contains("Intermediate") || bone.Contains("Distal")) && (!bone.Contains("Thumb"))))
				{
					float rightFingerNormalAngle = (float) (Mathf.Rad2Deg * Math.Asin(Vector3.Dot(NormalizeBone[bone], rightHandNormalPlane)) / (NormalizeBone[bone].magnitude * rightHandNormalPlane.magnitude));
					float angleOffset = -90.0f;
					if (bone.Contains("Proximal"))
					{
						float rightFingerAngle = (float) (Mathf.Rad2Deg * Math.Asin(Vector3.Dot(NormalizeBone[bone], rightHandNeutralPlane)) / (NormalizeBone[bone].magnitude * rightHandNeutralPlane.magnitude));
						if (rightFingerAngle > 0) // check if overstretching
							rightFingerNormalAngle = angleOffset;
					}
					else if (bone.Contains("Intermediate"))
					{
						if(rightFingerNormalAngle < boneAngle[bone.Replace("Intermediate", "Proximal")])  // check if overstretching
							rightFingerNormalAngle = boneAngle[bone.Replace("Intermediate", "Proximal")];
					}
					else if (bone.Contains("Distal"))
					{
						if(rightFingerNormalAngle < boneAngle[bone.Replace("Distal", "Intermediate")])  // check if overstretching
							rightFingerNormalAngle = boneAngle[bone.Replace("Distal", "Intermediate")];
					}
					boneAngle[bone] = rightFingerNormalAngle;
					continue;
				}
				else if (bone.Contains("Right") && bone.Contains("Thumb"))
				{
					float rightThumbNormalAngle = (float) (Mathf.Rad2Deg * Math.Asin(Vector3.Dot(NormalizeBone[bone], rightThumbNormalPlane)) / (NormalizeBone[bone].magnitude * rightThumbNormalPlane.magnitude));
					float offset = -90.0f;
					if (bone.Contains("Proximal"))
					{
						float rightThumbAngle = (float) (Mathf.Rad2Deg * Math.Asin(Vector3.Dot(NormalizeBone[bone], rightThumbPlane)) / (NormalizeBone[bone].magnitude * rightThumbPlane.magnitude));
						if (rightThumbAngle > 0) // check if overstretching
							rightThumbNormalAngle = offset;
						rotation1 = Quaternion.Euler(0, offset-rightThumbNormalAngle, 0); // -x..inside angle

					} else if (bone.Contains("Intermediate"))
					{
						if(rightThumbNormalAngle < boneAngle[bone.Replace("Intermediate", "Proximal")] - 10)  // check if overstretching
							rightThumbNormalAngle = boneAngle[bone.Replace("Intermediate", "Proximal")] - 10;
						rotation1 = Quaternion.Euler(0, offset - rightThumbNormalAngle, 0); // -y..inside angle
					} else if (bone.Contains("Distal"))
					{
						if(rightThumbNormalAngle < boneAngle[bone.Replace("Distal", "Intermediate")] - 40)  // check if overstretching
							rightThumbNormalAngle = boneAngle[bone.Replace("Distal", "Intermediate")] - 40;
						rotation1 = Quaternion.Euler(0, offset - rightThumbNormalAngle, 0); // -y..inside angle
					}
					BoneList[bone].rotation =  Quaternion.Lerp(BoneList[bone].rotation,(BoneList["RightHand"].rotation * rightThumbInitialRotation)* rotation1, handInterpolationFactor);
					boneAngle[bone] = rightThumbNormalAngle;
					continue;
				}
				else if (bone == "LeftUpperArm")
				{
					rotation1 = Quaternion.FromToRotation(new Vector3(1, 0, 0), new Vector3(-1, 1, 0));
					rotation2 = Quaternion.FromToRotation(new Vector3(-1, 1, 0), -NormalizeBone[bone]);
					rotation3 = Quaternion.EulerAngles(new Vector3(0,50,0)); // ellbows closer to body correction
					//BoneList[bone].rotation = Quaternion.Lerp(BoneList[bone].rotation,	rotation2 * rotation1, 0.1f);
					BoneList[bone].rotation = Quaternion.Lerp(BoneList[bone].rotation,	rotation3 * rotation2 * rotation1, 0.1f);
					continue;
				}
				else if (bone == "LeftLowerArm" )
				{
					rotation1 = Quaternion.FromToRotation(new Vector3(-1, 0, 0), new Vector3(0, 0, 1));
					rotation2 = Quaternion.EulerAngles(new Vector3(0,0,-30)); // -30...go into neutral rotation
					rotation3 = Quaternion.FromToRotation(new Vector3(0, 0, 1), NormalizeBone[bone]);
					Vector3 rotation3Euler = rotation3.ToEulerAngles() * Mathf.Rad2Deg;
					rotation3 =  Quaternion.Euler(rotation3Euler.x, rotation3Euler.y, 0);  // rotate into position without roll
					rotation4 =  Quaternion.AngleAxis(-leftHandRotationDelta*lowerArmRollRotationFactor, NormalizeBone[bone]); // roll rotation from hand; *3 because of Lerp and delta rotation
					BoneList[bone].rotation = Quaternion.Lerp(BoneList[bone].rotation,	rotation4 * rotation3 * rotation2 * rotation1, 0.1f);
					boneAngle[bone] = leftHandRotationDelta;
					continue;
				}
				else if (bone == "LeftHand" )
				{
					rotation1 = Quaternion.FromToRotation(new Vector3(-1, 0, 0), new Vector3(0, 0, 1));
					rotation2 = Quaternion.Euler(0, 0, 180); // intitial rotation
					rotation3 = Quaternion.FromToRotation(new Vector3(0, 0, 1), NormalizeBone[bone]);
					Vector3 rotation3Euler = rotation3.ToEulerAngles() * Mathf.Rad2Deg;
					rotation3 =  Quaternion.Euler(rotation3Euler.x, rotation3Euler.y, 0);
					rotation4 = Quaternion.AngleAxis(-leftHandRotationDelta, NormalizeBone[bone]);
					BoneList[bone].rotation = Quaternion.Lerp(BoneList[bone].rotation,rotation4*rotation3 *  rotation2*rotation1, handInterpolationFactor);
					continue;
					
				}
				else if ((bone.Contains("Left") && (bone.Contains("Proximal") || bone.Contains("Intermediate") || bone.Contains("Distal")) && (!bone.Contains("Thumb"))))
				{
					float leftFingerBellguardAngle = (float) (Mathf.Rad2Deg * Math.Asin(Vector3.Dot(NormalizeBone[bone], leftHandBellguardPlane)) / (NormalizeBone[bone].magnitude * leftHandBellguardPlane.magnitude));
					float angleOffset = -90.0f;
					if (bone.Contains("Proximal"))
					{
						float leftFingerAngle = (float) (Mathf.Rad2Deg * Math.Asin(Vector3.Dot(NormalizeBone[bone], leftHandNeutralPlane)) / (NormalizeBone[bone].magnitude * leftHandNeutralPlane.magnitude));
						if (leftFingerAngle > 0) // check if overstretching
							leftFingerBellguardAngle = angleOffset;
					}
					else if (bone.Contains("Intermediate"))
					{
						if(leftFingerBellguardAngle < boneAngle[bone.Replace("Intermediate", "Proximal")])  // check if overstretching
							leftFingerBellguardAngle = boneAngle[bone.Replace("Intermediate", "Proximal")];
					}
					else if (bone.Contains("Distal"))
					{
						if(leftFingerBellguardAngle < boneAngle[bone.Replace("Distal", "Intermediate")])  // check if overstretching
							leftFingerBellguardAngle = boneAngle[bone.Replace("Distal", "Intermediate")];
					}
					boneAngle[bone] = leftFingerBellguardAngle;
					continue;
				}
				else if (bone.Contains("Left") && bone.Contains("Thumb"))
				{
					float leftThumbNormalAngle = (float) (Mathf.Rad2Deg * Math.Asin(Vector3.Dot(NormalizeBone[bone], leftThumbNormalPlane)) / (NormalizeBone[bone].magnitude * leftThumbNormalPlane.magnitude));
					float offset = -90.0f;
					if (bone.Contains("Proximal"))
					{
						float leftThumbAngle = (float) (Mathf.Rad2Deg * Math.Asin(Vector3.Dot(NormalizeBone[bone], leftThumbPlane)) / (NormalizeBone[bone].magnitude * leftThumbPlane.magnitude));
						
						if (leftThumbAngle > 0) // check if overstretching
							leftThumbNormalAngle = offset;
						rotation1 = Quaternion.Euler(0, offset-leftThumbNormalAngle, 0); // -x..inside angle

					} else if (bone.Contains("Intermediate"))
					{
						
						if(leftThumbNormalAngle < boneAngle[bone.Replace("Intermediate", "Proximal")] - 10)  // check if overstretching
							leftThumbNormalAngle = boneAngle[bone.Replace("Intermediate", "Proximal")] - 10;
						rotation1 = Quaternion.Euler(0, offset - leftThumbNormalAngle, 0); // -y..inside angle
					} else if (bone.Contains("Distal"))
					{
					
						if(leftThumbNormalAngle < boneAngle[bone.Replace("Distal", "Intermediate")] - 40)  // check if overstretching
							leftThumbNormalAngle = boneAngle[bone.Replace("Distal", "Intermediate")] - 40;
						rotation1 = Quaternion.Euler(0, offset - leftThumbNormalAngle, 0); // -y..inside angle
					}
					BoneList[bone].rotation =  Quaternion.Lerp(BoneList[bone].rotation,(BoneList["LeftHand"].rotation * leftThumbInitialRotation)* rotation1, handInterpolationFactor);
					boneAngle[bone] = leftThumbNormalAngle;
					continue;
				}
			}
		}
		
		// Compute avg finger angle.
		Dictionary<string, float> averageFingerBoneAngle = new Dictionary<string, float>();
		foreach (string hand in hands)
		{
			foreach (string fingerBone in fingerBones)
			{
				averageFingerBoneAngle[hand + fingerBone] = computeAvarage4FingerAngle(hand, fingerBone);
			}
		}
		
		// Assign finger angles.
		float fingerDifferenceThreshold = 10.0f;
		Dictionary<string, float> handFingerAngleSign = new Dictionary<string, float>(){{"Left", -1.0f}, {"Right", -1.0f}};
		foreach (var hand in hands)
		{
			foreach (var finger in fingers4)
			{
				foreach (string fingerBone in fingerBones)
				{
					float angle = boneAngle[hand + finger + fingerBone];
					float average = averageFingerBoneAngle[hand + fingerBone];
					if (angle > average + fingerDifferenceThreshold)
						angle = average + fingerDifferenceThreshold;
					else if (angle < average - fingerDifferenceThreshold)
						angle = average - fingerDifferenceThreshold;
					Quaternion rotation = Quaternion.Euler(new Vector3(0, -90.0f + handFingerAngleSign[hand] * angle, 0));
					BoneList[hand + finger + fingerBone].rotation = Quaternion.Lerp(BoneList[hand + finger + fingerBone].rotation, BoneList[hand + "Hand"].rotation * rotation, handInterpolationFactor);
	                
				}
			}			
		}
		drawSkeleton();
	}


	

	void OnDrawGizmos() 
	{
		if(audioData != null)
		{
			GUIStyle style = new GUIStyle();
			style.normal.textColor = Color.black;
			Handles.Label(new Vector3(0, 1, 0), 
				"Time: " + audioData.time.ToString("00.0") + " / "  + 
				audioData.clip.length.ToString("00.0"), style);
		}
	}
}
