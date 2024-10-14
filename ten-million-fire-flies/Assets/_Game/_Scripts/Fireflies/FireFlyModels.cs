
using UnityEngine;
public class FireFlyModels : FireFlyEffect
{
	public override void OnBufferUpdate()
	{

		Args[0] = _instanceMesh.GetIndexCount(0);
		Args[1] = (uint)FireFlyManager.Instance.NearestCount;
		Args[2] = _instanceMesh.GetIndexStart(0);
		Args[3] = _instanceMesh.GetBaseVertex(0);

		ArgsBuffer.SetData(Args);
	}
}