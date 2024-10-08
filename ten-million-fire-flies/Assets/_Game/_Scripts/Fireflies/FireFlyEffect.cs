using UnityEngine;

public class FireFlyEffect : MonoBehaviour
{

	public readonly uint[] Args = { 0, 0, 0, 0, 0 };
	public ComputeBuffer ArgsBuffer { get; private set; }

	[SerializeField] protected Mesh _instanceMesh;
	[SerializeField] protected Material _instanceMaterial;

	public virtual void Awake()
	{
		ArgsBuffer = new ComputeBuffer(1, Args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
	}

	public virtual void OnBufferUpdate()
	{
		if (isActiveAndEnabled)
		{
			Args[0] = _instanceMesh.GetIndexCount(0);
			Args[1] = (uint)FireFlyManager.Instance.Count;
			Args[2] = _instanceMesh.GetIndexStart(0);
			Args[3] = _instanceMesh.GetBaseVertex(0);

			ArgsBuffer.SetData(Args);
		}
	}

	private void OnDisable()
	{
		ArgsBuffer?.Release();
		ArgsBuffer = null;
	}

	public virtual void Update()
	{
		Graphics.DrawMeshInstancedIndirect(_instanceMesh, 0, _instanceMaterial, new Bounds(Vector3.zero, Vector3.one * 2 * FireFlyManager.Instance.Radius), ArgsBuffer);
	}
}