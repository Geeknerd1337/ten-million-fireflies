using NoiseTest;
using TMPro;

using UnityEngine;
using UnityEngine.UI;

public class Level7 : MonoBehaviour
{
	[SerializeField] private Mesh _instanceMesh;
	[SerializeField] private Material _instanceMaterial;
	[SerializeField] private Slider _slider;
	[SerializeField] private TMP_Text _sliderValueText;


	private static int _countMultiplier = 1;
	private readonly uint[] _args = { 0, 0, 0, 0, 0 };
	private ComputeBuffer _argsBuffer;
	private int _count;

	private ComputeBuffer _positionBuffer1, _positionBuffer2;
	private int _cachedMultiplier = 1;
	private float _renderDistance = 100.0f;
	OpenSimplexNoise noiseGenerator;
	private void Start()
	{
		_count = 10000000;
		ApplyMultiplierUpdate(_countMultiplier, true);
		noiseGenerator = new OpenSimplexNoise();
		_argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
		UpdateBuffers();

		SceneTools.Instance.SetCountText(_count);
		SceneTools.Instance.SetNameText("GPU Instancing Indirect");
	}

	private float rotationSpeed = 10f; // Rotation speed in degrees per second
	private float rotationAngle = 0f;

	private void Update()
	{
		Graphics.DrawMeshInstancedIndirect(_instanceMesh, 0, _instanceMaterial, new Bounds(Vector3.zero, Vector3.one * 1000), _argsBuffer);

		Vector3 camPos = Camera.main.transform.position;
		_instanceMaterial.SetVector("_CameraPosition", camPos);

		rotationAngle += rotationSpeed * Time.deltaTime;


		// Create a quaternion representing the rotation around the up axis
		Quaternion rotation = Quaternion.Euler(rotationAngle * 0.5f, rotationAngle, rotationAngle * 0.25f);


		Matrix4x4 rotMatrix = Matrix4x4.Rotate(rotation);
		_instanceMaterial.SetMatrix("_Rotation", rotMatrix);

		if (Input.GetMouseButtonUp(0))
		{
			ApplyMultiplierUpdate(_cachedMultiplier);
			UpdateBuffers();
		}
	}

	private void OnDisable()
	{
		_positionBuffer1?.Release();
		_positionBuffer1 = null;

		_positionBuffer2?.Release();
		_positionBuffer2 = null;

		_argsBuffer?.Release();
		_argsBuffer = null;
	}

	public Vector4 test;

	public float MapToNegativeOneToOne(float value)
	{
		// Ensure the input value is clamped between 0 and 1
		value = Mathf.Clamp01(value);

		// Map value from range [0, 1] to range [-1, 1]
		return value * 2f - 1f;
	}

	private void UpdateBuffers()
	{
		// Positions
		_positionBuffer1?.Release();
		_positionBuffer2?.Release();
		_positionBuffer1 = new ComputeBuffer(_count, 16);
		_positionBuffer2 = new ComputeBuffer(_count, 16);

		var positions1 = new Vector4[_count];
		var positions2 = new Vector4[_count];

		// Grouping cubes into a bunch of spheres
		var offset = Vector3.zero;
		var batchIndex = 0;
		var batch = 0;
		int cubeSize = Mathf.CeilToInt(Mathf.Pow(_count, 1f / 3f));

		for (var i = 0; i < _count; i++)
		{
			// Calculate the x, y, z positions based on index and cubeSize
			int x = i % cubeSize;
			int y = (i / cubeSize) % cubeSize;
			int z = i / (cubeSize * cubeSize);



			float perlinX = MapToNegativeOneToOne(Mathf.PerlinNoise(x * test.x, z * test.x));
			float perlinY = MapToNegativeOneToOne(Mathf.PerlinNoise(z * test.x, x * test.x));
			float perlinZ = MapToNegativeOneToOne(Mathf.PerlinNoise1D(y * test.x));
			Vector3 perlinoffset = new Vector3(perlinX, perlinY, perlinZ) * test.z;



			// Create a 3D position in the cube by multiplying x, y, z by a step size (e.g., 10 units)
			Vector3 position = new Vector3(x * 10, (y * 10), z * 10) + perlinoffset;

			// Assign the calculated position to positions1 and positions2
			positions1[i] = position;



		}

		_positionBuffer1.SetData(positions1);
		_positionBuffer2.SetData(positions2);
		_instanceMaterial.SetBuffer("position_buffer_1", _positionBuffer1);
		_instanceMaterial.SetBuffer("position_buffer_2", _positionBuffer2);
		_instanceMaterial.SetColorArray("color_buffer", SceneTools.Instance.ColorArray);

		// Verts
		_args[0] = _instanceMesh.GetIndexCount(0);
		_args[1] = (uint)_count;
		_args[2] = _instanceMesh.GetIndexStart(0);
		_args[3] = _instanceMesh.GetBaseVertex(0);

		_argsBuffer.SetData(_args);
	}

	public void UpdateMultiplier(float val)
	{
		ApplyMultiplierUpdate(Mathf.CeilToInt(val));
	}

	private void ApplyMultiplierUpdate(int val, bool applySliderChange = false)
	{
		_sliderValueText.text = $"Multiplier: {val.ToString()}";
		_cachedMultiplier = val;
		if (applySliderChange) _slider.value = val;
	}
}