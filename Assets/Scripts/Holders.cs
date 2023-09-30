using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Holders
{
	class PositionRotation
	{
		public Vector3 Position { get; set; }
		public Vector3 Rotation { get; set; }

		public PositionRotation(Vector3 position, Vector3 rotation)
		{
			Position = position;
			Rotation = rotation;
		}
	}

	[System.Serializable]
	public class ListOfBlackScreens
	{
		public List<GameObject> allBlackScreens;

		public List<Fade> GetFades()
		{
			return (
				from blackScreen in allBlackScreens
				select blackScreen.GetComponentInChildren<Fade>()
			).ToList();
		}
	}
	
}
