
using UnityEngine;

[System.Serializable]
public class RTMaterial {
	public bool isDieletric; // 是否折射材质
	public float refractIndex = 1.0f; // 折射率，仅 isDieletric=true 时有用
	
	public float roughness; // 决定漫反射概率 1全漫反射 0全折射/镜面反射
	public Color albedoo; // 漫反射材质颜色

	public static RTMaterial Default = new RTMaterial {
		roughness = 1.0f,
		albedoo = Color.white
	};
}
