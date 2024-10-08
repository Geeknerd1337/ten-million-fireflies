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



	private List<FireFlyEffect> _fireFlyEffects { get; set; }

	[Header("Position Distribution Values")]
	public float Radius = 100f;
	public float NoiseSampleScale = 0.1f;
	public float NoisePositionScale = 1f;
	public AnimationCurve DistributionCurve = AnimationCurve.Linear(0, 0, 1, 1);

	private RTree fireflyRTree;

	// Example bounds for the RTree covering the world space
	private BoundingBox worldBounds = new BoundingBox(new Vector3(-4500, -4500, -4500), new Vector3(4500, 4500, 4500));

	public void Awake()
	{
		Instance = this;
		_fireFlyEffects = new List<FireFlyEffect>();
		_fireFlyEffects.AddRange(FindObjectsByType<FireFlyEffect>(FindObjectsSortMode.InstanceID));
		fireflyRTree = new RTree(worldBounds);

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


	// Modified UpdateNearestBuffer to use the new Vector4 array
	private void UpdateNearestBuffer()
	{
		Vector3 camPos = Camera.main.transform.position;
		var nearestPositions = GetFirefliesWithinRadius(camPos, 100f);
		NearestCount = nearestPositions.Count;

		if (NearestCount == 0)
		{
			return;
		}

		NearestFireFlyBuffer?.Release();
		NearestFireFlyBuffer = new ComputeBuffer(NearestCount, 16);

		Vector4[] positions = new Vector4[NearestCount];
		for (int i = 0; i < NearestCount; i++)
		{
			positions[i] = new Vector4(nearestPositions[i].x, nearestPositions[i].y, nearestPositions[i].z, 0);
		}

		NearestFireFlyBuffer.SetData(positions);
		Shader.SetGlobalBuffer("nearest_firefly_buffer", NearestFireFlyBuffer);

		OnBufferUpdate();
	}

	private void UpdateBuffers()
	{
		Debug.Log("UPDATING BUFFERS");

		FireFlyPositionBuffer?.Release();
		FireFlyPositionBuffer = new ComputeBuffer(Count, 16); // Assuming each Vector4 takes 16 bytes
		var positions = new Vector4[Count];

		fireflyRTree = new RTree(worldBounds); // Reset or rebuild the R-Tree

		for (var i = 0; i < Count; i++)
		{
			// Calculate position
			Vector3 perlinoffset = CalculatePerlinoffset(Count);
			Vector3 position = FireFlyUtils.RandomPointInSphereWithCurve(Count, Radius, DistributionCurve) + perlinoffset;

			// Store the position in the buffer
			positions[i] = new Vector4(position.x, position.y, position.z, 0);

			// Insert into R-Tree directly
			fireflyRTree.Insert(position);
		}

		FireFlyPositionBuffer.SetData(positions);
		Shader.SetGlobalBuffer("position_buffer_1", FireFlyPositionBuffer);

		OnBufferUpdate();
	}

	public List<Vector3> GetFirefliesWithinRadius(Vector3 center, float radius)
	{
		BoundingBox queryBox = new BoundingBox(center - new Vector3(radius, radius, radius), center + new Vector3(radius, radius, radius));
		return fireflyRTree.QueryRange(queryBox);
	}

}