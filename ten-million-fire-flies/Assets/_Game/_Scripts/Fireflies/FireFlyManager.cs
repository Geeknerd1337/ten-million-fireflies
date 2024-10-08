using System;
using System.Collections.Generic;
using UnityEngine;

public class FireFlyManager : MonoBehaviour
{
	public static FireFlyManager Instance { get; private set; }

	public ComputeBuffer FireFlyPositionBuffer { get; private set; }
	public ComputeBuffer NearestFireFlyBuffer { get; private set; }

	public int Count = 10000;
	public int NearestCount;




	public Action OnBufferUpdate;

	private OctreeNode<Vector3> fireflyOctree;
	public Vector3 OctreeBoundsCenter = Vector3.zero;
	public float OctreeBoundsSize = 200f;

	private List<FireFlyEffect> _fireFlyEffects { get; set; }

	[Header("Position Distribution Values")]
	public float Radius = 100f;
	public float NoiseSampleScale = 0.1f;
	public float NoisePositionScale = 1f;
	public AnimationCurve DistributionCurve = AnimationCurve.Linear(0, 0, 1, 1);

	public void Awake()
	{
		Instance = this;
		_fireFlyEffects = new List<FireFlyEffect>();
		_fireFlyEffects.AddRange(FindObjectsByType<FireFlyEffect>(FindObjectsSortMode.InstanceID));
		fireflyOctree = new OctreeNode<Vector3>(new Bounds(OctreeBoundsCenter, Vector3.one * OctreeBoundsSize), 0);

		//Call OnBufferUpdate when the buffer is updated
		OnBufferUpdate += () =>
		{
			foreach (var effect in _fireFlyEffects)
			{
				effect.OnBufferUpdate();
			}
		};
	}

	private void Start()
	{

		UpdateBuffers();
	}

	private void Update()
	{
		Vector3 camPos = Camera.main.transform.position;
		Shader.SetGlobalVector("_CameraPosition", camPos);

		UpdateNearestBuffer();


		if (Input.GetMouseButtonUp(0))
		{
			UpdateBuffers();
		}
	}

	private void OnDisable()
	{
		FireFlyPositionBuffer?.Release();
		FireFlyPositionBuffer = null;
	}

	public Vector3 CalculatePerlinoffset(int count)
	{
		float xIndex = (count + UnityEngine.Random.Range(0, count)) * NoiseSampleScale;
		float perlinX = FireFlyUtils.Remap(Mathf.PerlinNoise1D(xIndex));

		float yIndex = (count + UnityEngine.Random.Range(0, count)) * NoiseSampleScale;
		float perlinY = FireFlyUtils.Remap(Mathf.PerlinNoise1D(yIndex));

		float zIndex = (count + UnityEngine.Random.Range(0, count)) * NoiseSampleScale;
		float perlinZ = FireFlyUtils.Remap(Mathf.PerlinNoise1D(zIndex));



		Vector3 perlinoffset = new Vector3(perlinX, perlinY, perlinZ) * NoisePositionScale;

		return perlinoffset;
	}

	public Vector4[] GetPositionsWithinRadius(Vector3 center, float radius)
	{
		List<Vector3> result = new List<Vector3>();
		fireflyOctree.Retrieve(result, center, radius);



		Vector4[] vector4Result = new Vector4[result.Count];
		for (int i = 0; i < result.Count; i++)
		{
			vector4Result[i] = new Vector4(result[i].x, result[i].y, result[i].z, 0); // The 4th value (w) is set to 0 as it doesn't matter
		}
		return vector4Result;
	}

	// Modified UpdateNearestBuffer to use the new Vector4 array
	private void UpdateNearestBuffer()
	{
		Vector3 camPos = Camera.main.transform.position;
		var nearestPositions = GetPositionsWithinRadius(camPos, 10f);
		NearestCount = nearestPositions.Length;

		if (NearestCount == 0)
		{
			return;
		}

		NearestFireFlyBuffer?.Release();
		NearestFireFlyBuffer = new ComputeBuffer(NearestCount, 16); // Each Vector4 takes 16 bytes (4 * 4 bytes)

		NearestFireFlyBuffer.SetData(nearestPositions);
		Shader.SetGlobalBuffer("nearest_firefly_buffer", NearestFireFlyBuffer);

		OnBufferUpdate();
	}

	private void UpdateBuffers()
	{
		Debug.Log("UPDATING BUFFERS");
		// Positions
		FireFlyPositionBuffer?.Release();
		FireFlyPositionBuffer = new ComputeBuffer(Count, 16);

		var positions = new Vector4[Count];

		fireflyOctree = new OctreeNode<Vector3>(new Bounds(OctreeBoundsCenter, Vector3.one * OctreeBoundsSize), 0);

		// Grouping cubes into a bunch of spheres
		var offset = Vector3.zero;
		int cubeSize = Mathf.CeilToInt(Mathf.Pow(Count, 1f / 3f));

		for (var i = 0; i < Count; i++)
		{
			Vector3 perlinoffset = CalculatePerlinoffset(Count);
			Vector3 position = FireFlyUtils.RandomPointInSphereWithCurve(Count, Radius, DistributionCurve) + perlinoffset;

			// Assign the calculated position to positions1 and positions2
			positions[i] = position;
			fireflyOctree.Insert(position, position);
		}

		FireFlyPositionBuffer.SetData(positions);
		Shader.SetGlobalBuffer("position_buffer_1", FireFlyPositionBuffer);

		OnBufferUpdate();

	}

}