using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.U2D;
using UnityEngine.UI;

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo



public abstract class BoneControllerBase : MonoBehaviour
{
	public enum Interval // your custom enumeration
    {
    	Gruber1, 
    	Gruber2, 
    	Gruber3, 
    	Gruber4,
        Oliver1,
    };
	
	public Interval selectedInterval = Interval.Gruber1;
	[SerializeField] protected Animator animator;
	protected string speaker = "Gruber";
	protected string interval = "3821";  // 214126 101006 101011 214093 215981    HP: 101011, 102060,

	protected Dictionary<string, Transform> BoneList = new Dictionary<string, Transform>();
	protected Dictionary<string, float> boneAngle;
	
	protected static readonly int nrOfKeypoints = 49;
	protected readonly Vector3[] points = new Vector3[nrOfKeypoints];
	protected readonly Dictionary<string, Vector3> NormalizeBone = new Dictionary<string, Vector3>();
	protected List<string> inputBones;
	protected readonly List<Vector3[]> keypoints = new List<Vector3[]>();
	protected readonly Dictionary<string, string> bonesForward = new Dictionary<string, string>
	{
		//{"Neck", "RightShoulder"},
		//{"RightShoulder", "RightUpperArm"},
		{"RightUpperArm", "RightLowerArm"},
		{"RightLowerArm", "RightHand"},
		{"RightHand", "RightMiddleProximal"},
		//{"Neck", "LeftShoulder"},
		//{"LeftShoulder", "LeftUpperArm"},
		{"LeftUpperArm", "LeftLowerArm"},
		{"LeftLowerArm", "LeftHand"},
		{"LeftHand", "LeftMiddleProximal"},
		{"RightThumbProximal", "RightThumbIntermediate"},
		{"RightThumbIntermediate", "RightThumbDistal"},
		{"RightIndexProximal", "RightIndexIntermediate"},
		{"RightIndexIntermediate", "RightIndexDistal"},
		{"RightMiddleProximal", "RightMiddleIntermediate"},
		{"RightMiddleIntermediate", "RightMiddleDistal"},
		{"RightRingProximal", "RightRingIntermediate"},
		{"RightRingIntermediate", "RightRingDistal"},
		{"RightLittleProximal", "RightLittleIntermediate"},
		{"RightLittleIntermediate", "RightLittleDistal"},
		
		{"LeftThumbProximal", "LeftThumbIntermediate"},
		{"LeftThumbIntermediate", "LeftThumbDistal"},
		{"LeftIndexProximal", "LeftIndexIntermediate"},
		{"LeftIndexIntermediate", "LeftIndexDistal"},
		{"LeftMiddleProximal", "LeftMiddleIntermediate"},
		{"LeftMiddleIntermediate", "LeftMiddleDistal"},
		{"LeftRingProximal", "LeftRingIntermediate"},
		{"LeftRingIntermediate", "LeftRingDistal"},
		{"LeftLittleProximal", "LeftLittleIntermediate"},
		{"LeftLittleIntermediate", "LeftLittleDistal"},
	};
	
	protected static AudioSource audioData;
	protected AudioClip audioClip;
	
	protected float FrameRate = 15; // default: 15
	protected int currentFrameNr = 0;
	protected float startTime;
	protected Vector3 skeletonOffset = new Vector3(-0.5f, 0.8f, 0); // -1..right, 0.8 up
	protected string[] fingers4 = {"Index", "Middle", "Ring", "Little"};
	protected string[] hands = {"Left", "Right"};
	protected string[] fingerBones = {"Proximal", "Intermediate", "Distal"};
	protected Vector3 leftPosition = new Vector3(0.75f, 0, 0);
	protected Vector3 rightPosition = new Vector3(-0.75f, 0, 0);


	protected virtual void Start()
	{
		GetBones();
		loadKeypoints();
		PointUpdate();
		loadAudio();
		// Diff between keypoints and audio should be less then 0.5sec
		Debug.Assert(Math.Abs(audioData.clip.length - (keypoints.Count / FrameRate)) < 0.5); 
		SetBoneRot();
		audioData.Play();
		print("Audio playing? " + audioData.isPlaying);
	}

	protected virtual void Update()
	{
		print("running");
		PointUpdateByAudioTime();
		SetBoneRot();
	}
	protected void GetBones()
	{
		BoneList.Add("Hips", animator.GetBoneTransform(HumanBodyBones.Hips));
		BoneList.Add("LeftUpperLeg", animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg)); 
		BoneList.Add("LeftLowerLeg", animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg));
		BoneList.Add("LeftFoot", animator.GetBoneTransform(HumanBodyBones.LeftFoot)); 
		BoneList.Add("RightUpperLeg", animator.GetBoneTransform(HumanBodyBones.RightUpperLeg)); 
		BoneList.Add("RightLowerLeg", animator.GetBoneTransform(HumanBodyBones.RightLowerLeg)); 
		BoneList.Add("RightFoot", animator.GetBoneTransform(HumanBodyBones.RightFoot));
		BoneList.Add("Spine", animator.GetBoneTransform(HumanBodyBones.Spine));
		BoneList.Add("Chest", animator.GetBoneTransform(HumanBodyBones.Chest));
		BoneList.Add("Neck", animator.GetBoneTransform(HumanBodyBones.Neck)); 
		BoneList.Add("Head", animator.GetBoneTransform(HumanBodyBones.Head));  
		BoneList.Add("RightUpperArm", animator.GetBoneTransform(HumanBodyBones.RightUpperArm)); 
		BoneList.Add("RightLowerArm", animator.GetBoneTransform(HumanBodyBones.RightLowerArm)); 
		BoneList.Add("RightHand", animator.GetBoneTransform(HumanBodyBones.RightHand)); 
		BoneList.Add("LeftUpperArm", animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)); 
		BoneList.Add("LeftLowerArm", animator.GetBoneTransform(HumanBodyBones.LeftLowerArm));  
		BoneList.Add("LeftHand", animator.GetBoneTransform(HumanBodyBones.LeftHand)); 
		BoneList.Add("LeftShoulder", animator.GetBoneTransform(HumanBodyBones.LeftShoulder));  
		BoneList.Add("RightShoulder", animator.GetBoneTransform(HumanBodyBones.RightShoulder));  
		
		BoneList.Add("RightThumbProximal", animator.GetBoneTransform(HumanBodyBones.RightThumbProximal));  
		BoneList.Add("RightThumbIntermediate", animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate));  
		BoneList.Add("RightThumbDistal", animator.GetBoneTransform(HumanBodyBones.RightThumbDistal));
		BoneList.Add("RightIndexProximal", animator.GetBoneTransform(HumanBodyBones.RightIndexProximal));  
		BoneList.Add("RightIndexIntermediate", animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate));  
		BoneList.Add("RightIndexDistal", animator.GetBoneTransform(HumanBodyBones.RightIndexDistal)); 
		BoneList.Add("RightMiddleProximal", animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal)); 
		BoneList.Add("RightMiddleIntermediate", animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate)); 
		BoneList.Add("RightMiddleDistal", animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal));  
		BoneList.Add("RightRingProximal", animator.GetBoneTransform(HumanBodyBones.RightRingProximal));  
		BoneList.Add("RightRingIntermediate", animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate));  
		BoneList.Add("RightRingDistal", animator.GetBoneTransform(HumanBodyBones.RightRingDistal));  
		BoneList.Add("RightLittleProximal", animator.GetBoneTransform(HumanBodyBones.RightLittleProximal));  
		BoneList.Add("RightLittleIntermediate", animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate));  
		BoneList.Add("RightLittleDistal", animator.GetBoneTransform(HumanBodyBones.RightLittleDistal));  
		
		BoneList.Add("LeftThumbProximal", animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal));  
		BoneList.Add("LeftThumbIntermediate", animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate));  
		BoneList.Add("LeftThumbDistal", animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal));
		BoneList.Add("LeftIndexProximal", animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal));  
		BoneList.Add("LeftIndexIntermediate", animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate));  
		BoneList.Add("LeftIndexDistal", animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal)); 
		BoneList.Add("LeftMiddleProximal", animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal)); 
		BoneList.Add("LeftMiddleIntermediate", animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate)); 
		BoneList.Add("LeftMiddleDistal", animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal));  
		BoneList.Add("LeftRingProximal", animator.GetBoneTransform(HumanBodyBones.LeftRingProximal));  
		BoneList.Add("LeftRingIntermediate", animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate));  
		BoneList.Add("LeftRingDistal", animator.GetBoneTransform(HumanBodyBones.LeftRingDistal));  
		BoneList.Add("LeftLittleProximal", animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal));  
		BoneList.Add("LeftLittleIntermediate", animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate));  
		BoneList.Add("LeftLittleDistal", animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal));  
		
		boneAngle = new Dictionary<string, float>();
		inputBones = new List<string>(BoneList.Keys);
		foreach (string bone in inputBones)
		{
			boneAngle[bone] = 0;
			NormalizeBone[bone] = new Vector3(0,0,0);
		}

	}

	protected void loadKeypoints()
	{
		string path;
		path = Application.dataPath + Path.DirectorySeparatorChar + "Resources" + Path.DirectorySeparatorChar + speaker + Path.DirectorySeparatorChar + "prediction_interval_"+interval+".txt";

		StreamReader fi = new StreamReader(path);
		string line;
		while ((line = fi.ReadLine()) != null)
		{
			string[] kpsString = line.Split(' ');
			float[] kps = kpsString.Select(f => float.Parse(f)).ToArray();
			float[] kpsX = kps.Where((x, i) => i % 3 == 0).ToArray();
			float[] kpsY = kps.Where((x, i) => i % 3 == 1).ToArray();
			float[] kpsZ = kps.Where((x, i) => i % 3 == 2).ToArray();
			Debug.Assert(kpsX.Length == kpsY.Length && kpsY.Length == kpsZ.Length && kpsZ.Length == nrOfKeypoints); // == 49
			Vector3[] kpsVec = new Vector3[nrOfKeypoints];
			for(int i=0; i < nrOfKeypoints; i++)
				kpsVec[i] = new Vector3(kpsX[i], kpsY[i], kpsZ[i]);

			if (speaker == "Gruber") // turn shoulder rotation off
			{
				Vector3 shoulderLine = (kpsVec[1] - kpsVec[4]).normalized;
				Vector3 shoulderPlane = new Vector3(0, 0, 1);
				float shoulderRotation = (float) (Mathf.Rad2Deg * Math.Asin(Vector3.Dot(shoulderLine, shoulderPlane)) /
				                                  (shoulderLine.magnitude * shoulderPlane.magnitude));
				for (int i = 0; i < nrOfKeypoints; i++)
				{
					kpsVec[i] = rotatePointAroundAxis(kpsVec[i], -shoulderRotation, Vector3.up);
				}
				
			}
			keypoints.Add(kpsVec);
		}
		print("Number of frames is " + keypoints.Count + ".");
		print("Frames length is " + keypoints.Count / FrameRate + "s.");
		fi.Close();

	}

	protected virtual void loadAudio()
	{
		string path =  speaker + Path.DirectorySeparatorChar + interval;
		audioClip = Resources.Load<AudioClip>(path);
		audioData = GetComponent<AudioSource>();
		audioData.playOnAwake = false;
		audioData.clip = audioClip;
		print("Audio clip length is " +audioData.clip.length + "s.");
	}

	protected void PointUpdate()
	{
		if (currentFrameNr < keypoints.Count)
		{
			Vector3[] kpsVec3 = keypoints.ElementAt(currentFrameNr);
			// kps_vec3: 0 neck, 1-3 RArm, 4-6 LArm, 7-27 LHand , 28-48 RHand  (6==7, 3==28)
			for (int i = 0; i < kpsVec3.Length; i++)
			{
				points[i] = -kpsVec3[i];
			}
			
			NormalizeBone["RightShoulder"] = (points[1] - points[0]).normalized;
			NormalizeBone["RightUpperArm"] = (points[2] - points[1]).normalized;
			NormalizeBone["RightLowerArm"] = (points[3] - points[2]).normalized;
			NormalizeBone["LeftShoulder"] = (points[4] - points[0]).normalized;
			NormalizeBone["LeftUpperArm"] = (points[5] - points[4]).normalized;
			NormalizeBone["LeftLowerArm"] = (points[6] - points[5]).normalized;
			//NormalizeBone["LeftHand"] = (points[19] - points[6]).normalized;
			//   (LWrist - 19 - 18 - 17 - 16)
			// Interpolation between fingers:
			NormalizeBone["LeftHand"] = ((points[15] + points[19] + points[23] + points[27]) / 4.0f - points[6]).normalized; 
			//NormalizeBone["RightHand"] = (points[40]-points[3]).normalized;
			////  (RWrist - Middle 40 - 39 - 38 - 37)
			// Interpolation between fingers:
			NormalizeBone["RightHand"] = ((points[36] + points[40] + points[44] + points[48]) / 4.0f -points[3]).normalized;  //  (RWrist - Middle 40 - 39 - 38 - 37) // TODO interpolation between fingers

			
			NormalizeBone["LeftThumbProximal"] = (points[10]-points[11]).normalized;
			NormalizeBone["LeftThumbIntermediate"] = (points[9]-points[10]).normalized;
			NormalizeBone["LeftThumbDistal"] = (points[8]-points[9]).normalized;
			
			NormalizeBone["LeftIndexProximal"] = (points[14]-points[15]).normalized;
			NormalizeBone["LeftIndexIntermediate"] = (points[13]-points[14]).normalized;
			NormalizeBone["LeftIndexDistal"] = (points[12]-points[13]).normalized;
			
			NormalizeBone["LeftMiddleProximal"] = (points[18]-points[19]).normalized;
			NormalizeBone["LeftMiddleIntermediate"] = (points[17]-points[18]).normalized;
			NormalizeBone["LeftMiddleDistal"] = (points[16]-points[17]).normalized;
			
			NormalizeBone["LeftRingProximal"] = (points[22]-points[23]).normalized;
			NormalizeBone["LeftRingIntermediate"] = (points[21]-points[22]).normalized;
			NormalizeBone["LeftRingDistal"] = (points[20]-points[21]).normalized;
			
			NormalizeBone["LeftLittleProximal"] = (points[26]-points[27]).normalized;
			NormalizeBone["LeftLittleIntermediate"] = (points[25]-points[26]).normalized;
			NormalizeBone["LeftLittleDistal"] = (points[24]-points[25]).normalized;
			
			
			NormalizeBone["RightThumbProximal"] = (points[31]-points[32]).normalized;
			NormalizeBone["RightThumbIntermediate"] = (points[30]-points[31]).normalized;
			NormalizeBone["RightThumbDistal"] = (points[29]-points[30]).normalized;
			
			NormalizeBone["RightIndexProximal"] = (points[35]-points[36]).normalized;
			NormalizeBone["RightIndexIntermediate"] = (points[34]-points[35]).normalized;
			NormalizeBone["RightIndexDistal"] = (points[33]-points[34]).normalized;
			
			NormalizeBone["RightMiddleProximal"] = (points[39]-points[40]).normalized;
			NormalizeBone["RightMiddleIntermediate"] = (points[38]-points[39]).normalized;
			NormalizeBone["RightMiddleDistal"] = (points[37]-points[38]).normalized;
			
			NormalizeBone["RightRingProximal"] = (points[43]-points[44]).normalized;
			NormalizeBone["RightRingIntermediate"] = (points[42]-points[43]).normalized;
			NormalizeBone["RightRingDistal"] = (points[41]-points[42]).normalized;
			
			NormalizeBone["RightLittleProximal"] = (points[47]-points[48]).normalized;
			NormalizeBone["RightLittleIntermediate"] = (points[46]-points[47]).normalized;
			NormalizeBone["RightLittleDistal"] = (points[45]-points[46]).normalized;
			
			currentFrameNr++;
			//drawSkeleton();
		}
		else
		{
			print("Finished. Stop Player.");
			//print("Audio playback position: " + (audioData.time) + "s.");
			EditorApplication.ExecuteMenuItem("Edit/Play");
		}
	}
	protected void PointUpdateByAudioTime()
	{
		if (audioData.time > currentFrameNr * (1.0f / FrameRate))
			PointUpdate();
	}
	
	protected void PointUpdateByStartTime()
	{
		if ((Time.time - startTime) > currentFrameNr * (1.0f / FrameRate))
			PointUpdate();
	}
	
	protected abstract void SetBoneRot();
	

	void DrawLine(Vector3 s, Vector3 e, Color c)
	{
		Debug.DrawLine(s, e, c);
	}
	protected void DrawRay(Vector3 s, Vector3 d, Color c)
	{
		Debug.DrawRay(s, d, c);
	}

	protected void DrawPlane(Vector3 position, Vector3 normal, float size = 0.1f) {
 
		Vector3 v3;
		if (normal.normalized != Vector3.forward)
			v3 = Vector3.Cross(normal, Vector3.forward).normalized * normal.magnitude* size;
		else
			v3 = Vector3.Cross(normal, Vector3.up).normalized * normal.magnitude * size;
     
		var corner0 = position + v3;
		var corner2 = position - v3;
		var q = Quaternion.AngleAxis(90.0f, normal);
		v3 = q * v3;
		var corner1 = position + v3;
		var corner3 = position - v3;
 
		Debug.DrawLine(corner0, corner2, Color.green);
		Debug.DrawLine(corner1, corner3, Color.green);
		Debug.DrawLine(corner0, corner1, Color.green);
		Debug.DrawLine(corner1, corner2, Color.green);
		Debug.DrawLine(corner2, corner3, Color.green);
		Debug.DrawLine(corner3, corner0, Color.green);
		Debug.DrawRay(position, normal, Color.red);
	}
	
	protected void drawSkeleton()
	{
		DrawLine(points[0] + skeletonOffset, points[1] + skeletonOffset, Color.blue);
		DrawLine(points[1] + skeletonOffset, points[2] + skeletonOffset, Color.blue);
		DrawLine(points[2] + skeletonOffset, points[3] + skeletonOffset, Color.blue);
		DrawLine(points[0] + skeletonOffset, points[4] + skeletonOffset, Color.blue);
		DrawLine(points[4] + skeletonOffset, points[5] + skeletonOffset, Color.blue);
		DrawLine(points[5] + skeletonOffset, points[6] + skeletonOffset, Color.blue);
		
		DrawLine(points[8] + skeletonOffset, points[9] + skeletonOffset, Color.blue); // LThumb
		DrawLine(points[9] + skeletonOffset, points[10] + skeletonOffset, Color.blue);
		DrawLine(points[10] + skeletonOffset, points[11] + skeletonOffset, Color.blue);
		DrawLine(points[11] + skeletonOffset, points[6] + skeletonOffset, Color.blue);
		
		DrawLine(points[12] + skeletonOffset, points[13] + skeletonOffset, Color.cyan); //LIndex
		DrawLine(points[13] + skeletonOffset, points[14] + skeletonOffset, Color.cyan);
		DrawLine(points[14] + skeletonOffset, points[15] + skeletonOffset, Color.cyan);
		DrawLine(points[15] + skeletonOffset, points[6] + skeletonOffset, Color.cyan);
		
		DrawLine(points[16] + skeletonOffset, points[17] + skeletonOffset, Color.green); //LMiddle
		DrawLine(points[17] + skeletonOffset, points[18] + skeletonOffset, Color.green);
		DrawLine(points[18] + skeletonOffset, points[19] + skeletonOffset, Color.green);
		DrawLine(points[19] + skeletonOffset, points[6] + skeletonOffset, Color.green);
		
		DrawLine(points[20] + skeletonOffset, points[21] + skeletonOffset, Color.magenta); //LRing
		DrawLine(points[21] + skeletonOffset, points[22] + skeletonOffset, Color.magenta);
		DrawLine(points[22] + skeletonOffset, points[23] + skeletonOffset, Color.magenta);
		DrawLine(points[23] + skeletonOffset, points[6] + skeletonOffset, Color.magenta);
		
		DrawLine(points[24] + skeletonOffset, points[25] + skeletonOffset, Color.red); //LLittle
		DrawLine(points[25] + skeletonOffset, points[26] + skeletonOffset, Color.red);
		DrawLine(points[26] + skeletonOffset, points[27] + skeletonOffset, Color.red);
		DrawLine(points[27] + skeletonOffset, points[6] + skeletonOffset, Color.red);
		
		DrawLine(points[29] + skeletonOffset, points[30] + skeletonOffset, Color.blue); // RThumb
		DrawLine(points[30] + skeletonOffset, points[31] + skeletonOffset, Color.blue);
		DrawLine(points[31] + skeletonOffset, points[32] + skeletonOffset, Color.blue);
		DrawLine(points[32] + skeletonOffset, points[3] + skeletonOffset, Color.blue);
		
		DrawLine(points[33] + skeletonOffset, points[34] + skeletonOffset, Color.cyan); //RIndex
		DrawLine(points[34] + skeletonOffset, points[35] + skeletonOffset, Color.cyan);
		DrawLine(points[35] + skeletonOffset, points[36] + skeletonOffset, Color.cyan);
		DrawLine(points[36] + skeletonOffset, points[3] + skeletonOffset, Color.cyan);
		
		DrawLine(points[37] + skeletonOffset, points[38] + skeletonOffset, Color.green); //RMiddle
		DrawLine(points[38] + skeletonOffset, points[39] + skeletonOffset, Color.green);
		DrawLine(points[39] + skeletonOffset, points[40] + skeletonOffset, Color.green);
		DrawLine(points[40] + skeletonOffset, points[3] + skeletonOffset, Color.green);
		
		DrawLine(points[41] + skeletonOffset, points[42] + skeletonOffset, Color.magenta); //RRing
		DrawLine(points[42] + skeletonOffset, points[43] + skeletonOffset, Color.magenta);
		DrawLine(points[43] + skeletonOffset, points[44] + skeletonOffset, Color.magenta);
		DrawLine(points[44] + skeletonOffset, points[3] + skeletonOffset, Color.magenta);
		
		DrawLine(points[45] + skeletonOffset, points[46] + skeletonOffset, Color.red); //RLittle
		DrawLine(points[46] + skeletonOffset, points[47] + skeletonOffset, Color.red);
		DrawLine(points[47] + skeletonOffset, points[48] + skeletonOffset, Color.red);
		DrawLine(points[48] + skeletonOffset, points[3] + skeletonOffset, Color.red);
	}
	protected float computeAvarage4FingerAngle(string hand, string fingerBone)
	{
		float angleSum = 0.0f;
		foreach (string finger in fingers4)
		{
			angleSum += boneAngle[hand + finger + fingerBone];
		}

		return angleSum / fingers4.Length;
	}
	
	// rotates point around axis assuming that the axis goes through the origin
	Vector3 rotatePointAroundAxis(Vector3 point, float angle, Vector3 axis) {
		Quaternion q = Quaternion.AngleAxis(angle, axis);
		return q * point; 
	}
}