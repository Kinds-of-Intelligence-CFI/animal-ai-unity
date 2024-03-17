using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using PrefabInterface;

/// <summary>
/// Handles the method of modifying and spawning game objects in the environment.
/// </summary>
public class Prefab : MonoBehaviour, IPrefab
{
	[Header("Prefab Configuration")]
	public Vector2 rotationRange;
	public Vector3 sizeMin, sizeMax, ratioSize;
	public bool canRandomizeColor = true, textureUVOverride = false, typicalOrigin = true;
	public float sizeAdjustment = 0.999f;

	protected Renderer[] _renderers;
	protected float _height;

	private void Awake()
	{
		_renderers = GetComponentsInChildren<Renderer>();
	}

	public virtual void SetColor(Vector3 color)
	{
		if (!canRandomizeColor) return;

		Color newColor = new Color(
			color.x >= 0 ? color.x / 255f : Random.Range(0f, 1f),
			color.y >= 0 ? color.y / 255f : Random.Range(0f, 1f),
			color.z >= 0 ? color.z / 255f : Random.Range(0f, 1f),
			1f
		);

		foreach (var renderer in _renderers)
		{
			if (renderer.material.GetFloat("_Surface") != 1) // Not Transparent
			{
				renderer.material.color = newColor;
			}
		}
	}

	public virtual void SetSize(Vector3 size)
	{
		Vector3 clippedSize = Vector3.Max(sizeMin, Vector3.Min(sizeMax, size)) * sizeAdjustment;
		Vector3 newSize = new Vector3(
			size.x < 0 ? Random.Range(sizeMin.x, sizeMax.x) : clippedSize.x,
			size.y < 0 ? Random.Range(sizeMin.y, sizeMax.y) : clippedSize.y,
			size.z < 0 ? Random.Range(sizeMin.z, sizeMax.z) : clippedSize.z
		);

		// Apply ratioSize correctly as a multiplication to each dimension.
		newSize.x *= ratioSize.x;
		newSize.y *= ratioSize.y;
		newSize.z *= ratioSize.z;

		_height = newSize.y;
		transform.localScale = newSize;

		if (textureUVOverride) RescaleUVs();
	}

	public virtual Vector3 GetRotation(float rotationY)
	{
		return new Vector3(
			0,
			rotationY < 0 ? Random.Range(rotationRange.x, rotationRange.y) : rotationY,
			0
		);
	}

	public virtual Vector3 GetPosition(Vector3 position, Vector3 boundingBox, float rangeX, float rangeZ)
	{
		float xOut = position.x < 0 ? Random.Range(boundingBox.x, rangeX - boundingBox.x) : Mathf.Max(0, Mathf.Min(position.x, rangeX));
		float yOut = Mathf.Max(position.y, 0);
		float zOut = position.z < 0 ? Random.Range(boundingBox.z, rangeZ - boundingBox.z) : Mathf.Max(0, Mathf.Min(position.z, rangeZ));

		return new Vector3(xOut, AdjustY(yOut), zOut);
	}

	protected virtual float AdjustY(float yIn)
	{
		return yIn + (typicalOrigin ? (_height / 2) : 0) + 0.01f;
	}

	protected virtual void RescaleUVs(bool child = false, GameObject childOverride = null)
	{
		Renderer R =
			(child) ? childOverride.GetComponent<Renderer>() : this.GetComponent<Renderer>();
		MeshFilter MF =
			(child) ? childOverride.GetComponent<MeshFilter>() : this.GetComponent<MeshFilter>();
		if (R != null && R.material.GetTexture("_BaseMap") != null)
		{
			MF.sharedMesh = Instantiate<Mesh>(MF.mesh);
			Mesh MESH = MF.sharedMesh;

			Transform T = /*(child) ? transform.parent :*/
			transform;

			Vector2[] uvs = new Vector2[MESH.uv.Length];
			Dictionary<Vector3, Vector2Int> uvStretchLookup = new Dictionary<Vector3, Vector2Int>
			{
				{ new Vector3(0f, 0f, 1f), new Vector2Int(0, 1) },
				{ new Vector3(0f, 1f, 0f), new Vector2Int(0, 2) },
				{ new Vector3(0f, 0f, -1f), new Vector2Int(0, 1) },
				{ new Vector3(0f, -1f, 0f), new Vector2Int(0, 2) },
				{ new Vector3(-1f, 0f, 0f), new Vector2Int(2, 1) },
				{ new Vector3(1f, 0f, 0f), new Vector2Int(2, 1) }
			};
			Vector2Int n;
			bool b;
			Vector2Int d = new Vector2Int(0, 1);
			for (int i = 0; i < uvs.Length; ++i)
			{
				b = uvStretchLookup.TryGetValue(MESH.normals[i], out n);
				if (b)
				{
					uvs[i].x = (MESH.uv[i].x > 0) ? T.localScale[n.x] : 0;
					uvs[i].y = (MESH.uv[i].y > 0) ? T.localScale[n.y] : 0;
				}
				else
				{
					uvs[i].x = (MESH.uv[i].x > 0) ? T.localScale[0] : 0;
					uvs[i].y =
						(MESH.uv[i].y > 0)
							? Mathf.Sqrt(
								Mathf.Pow(T.localScale[1], 2) + Mathf.Pow(T.localScale[2], 2)
							)
							: 0;
				}
			}
			MESH.uv = uvs;
		}
		else if (!child)
		{
			for (int i = 0; i < transform.childCount; ++i)
			{
				RescaleUVs(true, (transform.GetChild(i).gameObject));
			}
		}
	}

	protected void SetOption<T>(T value, string optionName)
	{
		Debug.Log($"{optionName} set to {value} in Prefab.");
	}

	// Unused methods
	public virtual void SetDelay(float v) => SetOption(v, nameof(SetDelay));
	public virtual void SetInitialValue(float v) => SetOption(v, nameof(SetInitialValue));
	public virtual void SetFinalValue(float v) => SetOption(v, nameof(SetFinalValue));
	public virtual void SetChangeRate(float v) => SetOption(v, nameof(SetChangeRate));
	public virtual void SetSpawnCount(float v) => SetOption(v, nameof(SetSpawnCount));
	public virtual void SetTimeBetweenSpawns(float v) => SetOption(v, nameof(SetTimeBetweenSpawns));
	public virtual void SetRipenTime(float v) => SetOption(v, nameof(SetRipenTime));
	public virtual void SetDoorDelay(float v) => SetOption(v, nameof(SetDoorDelay));
	public virtual void SetTimeBetweenDoorOpens(float v) => SetOption(v, nameof(SetTimeBetweenDoorOpens));
	public virtual void SetSpawnColor(Vector3 v) => SetOption(v, nameof(SetSpawnColor));

}
