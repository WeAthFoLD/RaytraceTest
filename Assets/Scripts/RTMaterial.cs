
using UnityEngine;

[System.Serializable]
public class RTMaterial {
	public float roughness;
	public Color albedoo;

	public static RTMaterial Default = new RTMaterial {
		roughness = 1.0f,
		albedoo = Color.white
	};
}
