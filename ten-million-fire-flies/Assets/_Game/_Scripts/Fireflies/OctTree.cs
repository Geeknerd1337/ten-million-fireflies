using System.Collections.Generic;
using UnityEngine;

public class OctreeNode<T>
{
	private const int MAX_OBJECTS = 16; // Maximum number of objects before splitting
	private const int MAX_LEVELS = 4;  // Maximum depth of the tree

	private List<T> objects;           // Objects in this node
	private Bounds bounds;             // Bounding box of this node
	private OctreeNode<T>[] nodes;     // Children of this node
	private int level;                 // Depth level of this node

	public OctreeNode(Bounds bounds, int level)
	{
		this.bounds = bounds;
		this.level = level;
		objects = new List<T>();
		nodes = new OctreeNode<T>[8];
	}

	public void Insert(T obj, Vector3 position)
	{
		if (nodes[0] != null)
		{
			int index = GetChildIndex(position);
			if (index != -1)
			{
				nodes[index].Insert(obj, position);
				return;
			}
		}

		objects.Add(obj);

		if (objects.Count > MAX_OBJECTS && level < MAX_LEVELS)
		{
			if (nodes[0] == null) Split();
			for (int i = objects.Count - 1; i >= 0; i--)
			{
				int index = GetChildIndex(position);
				if (index != -1)
				{
					nodes[index].Insert(objects[i], position);
					objects.RemoveAt(i);
				}
			}
		}
	}

	private void Split()
	{
		Vector3 size = bounds.size / 2f;
		Vector3 center = bounds.center;

		nodes[0] = new OctreeNode<T>(new Bounds(center + new Vector3(-size.x, size.y, size.z), size), level + 1);
		nodes[1] = new OctreeNode<T>(new Bounds(center + new Vector3(size.x, size.y, size.z), size), level + 1);
		nodes[2] = new OctreeNode<T>(new Bounds(center + new Vector3(-size.x, size.y, -size.z), size), level + 1);
		nodes[3] = new OctreeNode<T>(new Bounds(center + new Vector3(size.x, size.y, -size.z), size), level + 1);
		nodes[4] = new OctreeNode<T>(new Bounds(center + new Vector3(-size.x, -size.y, size.z), size), level + 1);
		nodes[5] = new OctreeNode<T>(new Bounds(center + new Vector3(size.x, -size.y, size.z), size), level + 1);
		nodes[6] = new OctreeNode<T>(new Bounds(center + new Vector3(-size.x, -size.y, -size.z), size), level + 1);
		nodes[7] = new OctreeNode<T>(new Bounds(center + new Vector3(size.x, -size.y, -size.z), size), level + 1);
	}

	private int GetChildIndex(Vector3 position)
	{
		// Determine which octant the object belongs to
		int index = -1;
		bool left = (position.x < bounds.center.x);
		bool bottom = (position.y < bounds.center.y);
		bool back = (position.z < bounds.center.z);

		if (left)
		{
			if (bottom) index = back ? 6 : 4;
			else index = back ? 2 : 0;
		}
		else
		{
			if (bottom) index = back ? 7 : 5;
			else index = back ? 3 : 1;
		}

		return index;
	}

	public void Retrieve(List<T> returnObjects, Vector3 position, float radius)
	{
		if (!bounds.Intersects(new Bounds(position, Vector3.one * radius * 2f)))
			return;

		foreach (var obj in objects)
		{
			// Add distance check if needed
			returnObjects.Add(obj);
		}

		if (nodes[0] != null)
		{
			for (int i = 0; i < 8; i++)
			{
				nodes[i].Retrieve(returnObjects, position, radius);
			}
		}
	}
}
