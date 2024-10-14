using System.Collections.Generic;
using UnityEngine;

// Class representing a bounding box (Minimum Bounding Rectangle - MBR)
public class BoundingBox
{
	public Vector3 Min;
	public Vector3 Max;

	public BoundingBox(Vector3 min, Vector3 max)
	{
		Min = min;
		Max = max;
	}

	// Check if this bounding box intersects with another (used for range queries)
	public bool Intersects(BoundingBox other)
	{
		return !(Min.x > other.Max.x || Max.x < other.Min.x ||
				 Min.y > other.Max.y || Max.y < other.Min.y ||
				 Min.z > other.Max.z || Max.z < other.Min.z);
	}

	// Check if a point is within this bounding box
	public bool Contains(Vector3 point)
	{
		return (point.x >= Min.x && point.x <= Max.x &&
				point.y >= Min.y && point.y <= Max.y &&
				point.z >= Min.z && point.z <= Max.z);
	}
}

// RTree Node
public class RTreeNode
{
	public List<Vector3> Points;
	public BoundingBox Box;
	public List<RTreeNode> Children;

	public RTreeNode(BoundingBox box)
	{
		Points = new List<Vector3>();
		Box = box;
		Children = new List<RTreeNode>();
	}

	public bool IsLeaf => Children.Count == 0;
}

// R-Tree class
public class RTree
{
	private RTreeNode _root;
	private int _maxPointsPerNode = 64; // Adjust based on your data
	private int _maxDepth = 10;

	public RTree(BoundingBox worldBounds)
	{
		_root = new RTreeNode(worldBounds);
	}

	// Insert a point into the tree
	public void Insert(Vector3 point)
	{
		Insert(_root, point, 0);
	}

	private void Insert(RTreeNode node, Vector3 point, int depth)
	{
		if (node.IsLeaf)
		{
			// If it's a leaf node, add the point directly
			node.Points.Add(point);

			// If this node exceeds max points, split it
			if (node.Points.Count > _maxPointsPerNode && depth < _maxDepth)
			{
				SplitNode(node);
			}
		}
		else
		{
			// Recursively insert into the correct child
			RTreeNode bestChild = null;
			foreach (var child in node.Children)
			{
				if (child.Box.Contains(point))
				{
					bestChild = child;
					break;
				}
			}

			if (bestChild != null)
			{
				Insert(bestChild, point, depth + 1);
			}
		}
	}

	// Split the node into smaller nodes
	private void SplitNode(RTreeNode node)
	{
		Vector3 center = (node.Box.Min + node.Box.Max) / 2;
		Vector3 size = (node.Box.Max - node.Box.Min) / 2;

		// Create 8 child nodes (like an octree split)
		for (int i = 0; i < 8; i++)
		{
			Vector3 min = node.Box.Min;
			Vector3 max = center;

			if ((i & 1) != 0) { min.x = center.x; max.x = node.Box.Max.x; }
			if ((i & 2) != 0) { min.y = center.y; max.y = node.Box.Max.y; }
			if ((i & 4) != 0) { min.z = center.z; max.z = node.Box.Max.z; }

			var childBox = new BoundingBox(min, max);
			node.Children.Add(new RTreeNode(childBox));
		}

		// Redistribute points into the new children
		foreach (var point in node.Points)
		{
			foreach (var child in node.Children)
			{
				if (child.Box.Contains(point))
				{
					child.Points.Add(point);
					break;
				}
			}
		}

		node.Points.Clear();
	}

	// Query points within a bounding box
	public List<Vector3> QueryRange(BoundingBox range)
	{
		List<Vector3> results = new List<Vector3>();
		QueryRange(_root, range, results);
		return results;
	}

	private void QueryRange(RTreeNode node, BoundingBox range, List<Vector3> results)
	{
		if (!node.Box.Intersects(range)) return;

		if (node.IsLeaf)
		{
			foreach (var point in node.Points)
			{
				if (range.Contains(point))
				{
					results.Add(point);
				}
			}
		}
		else
		{
			foreach (var child in node.Children)
			{
				QueryRange(child, range, results);
			}
		}
	}
}
