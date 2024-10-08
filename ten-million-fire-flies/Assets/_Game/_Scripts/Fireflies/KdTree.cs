using System.Collections.Generic;
using UnityEngine;
public class KdTreeNode
{
	public Vector3 Position;
	public KdTreeNode Left;
	public KdTreeNode Right;

	public KdTreeNode(Vector3 position)
	{
		this.Position = position;
		this.Left = null;
		this.Right = null;
	}
}

public class KdTree
{
	private KdTreeNode Root;
	private List<Vector3> DelayedInsertionPoints = new List<Vector3>();
	private int InsertionBatchSize = 1000; // Control batch size for insertion

	public KdTree(List<Vector3> points)
	{
		Root = BuildTree(points, 0);
	}

	private KdTreeNode BuildTree(List<Vector3> points, int depth)
	{
		if (points.Count == 0) return null;

		// Alternate the axis: 0 -> x, 1 -> y, 2 -> z
		int axis = depth % 3;

		// Sort points based on the current axis
		points.Sort((a, b) => axis == 0 ? a.x.CompareTo(b.x) : axis == 1 ? a.y.CompareTo(b.y) : a.z.CompareTo(b.z));

		// Choose the median point
		int medianIndex = points.Count / 2;
		Vector3 medianPoint = points[medianIndex];

		// Recursively build the tree
		KdTreeNode node = new KdTreeNode(medianPoint)
		{
			Left = BuildTree(points.GetRange(0, medianIndex), depth + 1),
			Right = BuildTree(points.GetRange(medianIndex + 1, points.Count - medianIndex - 1), depth + 1)
		};

		return node;
	}

	// To be used in querying
	public KdTreeNode GetRoot()
	{
		return Root;
	}

	public List<Vector3> SearchNeighbors(Vector3 target, float radius)
	{
		List<Vector3> result = new List<Vector3>();
		SearchNearest(Root, target, radius, 0, result);
		return result;
	}

	private void SearchNearest(KdTreeNode node, Vector3 target, float radius, int depth, List<Vector3> result)
	{
		if (node == null) return;

		// Check if current node is within the radius
		if (Vector3.Distance(node.Position, target) <= radius)
		{
			result.Add(node.Position);
		}

		int axis = depth % 3;

		// Decide which side to search
		bool searchLeft = axis == 0 ? target.x < node.Position.x :
						  axis == 1 ? target.y < node.Position.y :
						  target.z < node.Position.z;

		// Recursively search the side that the target is on
		if (searchLeft)
		{
			SearchNearest(node.Left, target, radius, depth + 1, result);
		}
		else
		{
			SearchNearest(node.Right, target, radius, depth + 1, result);
		}

		// Also check the other side if the splitting plane is within the search radius
		float splitDistance = axis == 0 ? Mathf.Abs(target.x - node.Position.x) :
							  axis == 1 ? Mathf.Abs(target.y - node.Position.y) :
							  Mathf.Abs(target.z - node.Position.z);

		if (splitDistance <= radius)
		{
			if (searchLeft)
			{
				SearchNearest(node.Right, target, radius, depth + 1, result);
			}
			else
			{
				SearchNearest(node.Left, target, radius, depth + 1, result);
			}
		}
	}
}
