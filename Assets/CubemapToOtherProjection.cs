using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CubemapToEquidistanceProjection : MonoBehaviour
{
	public RenderTexture RenderTarget;
	public int CubemapSize = 1024;
	public bool RenderInStereo = false;
	public float StereoSeparation = 0.065f;

	private Camera _camera;
	private Material _material;
	private RenderTexture _cubemap;

	// Use this for initialization
	void Start()
	{
		_camera = GetComponent<Camera>();

		_material = new Material(Shader.Find("Conversion/CubemapToEquidistanceProjection"));
		_cubemap = new RenderTexture(CubemapSize, CubemapSize, 24, RenderTextureFormat.ARGB32);
		_cubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
	}

	// Update is called once per frame
	void Update()
	{
		if (RenderTarget == null)
		{
			return;
		}

		var matrix = Matrix4x4.Rotate(_camera.transform.localRotation);
		_material.SetMatrix("_Matrix", matrix);
		_material.EnableKeyword("ANGLEFUNC_EQUISOLIDANGLE");

		if (RenderInStereo)
		{
			var tmpStereoSepration = _camera.stereoSeparation;
			var tmpStereoTargetEye = _camera.stereoTargetEye;
			_camera.stereoSeparation = StereoSeparation;
			_camera.stereoTargetEye = StereoTargetEyeMask.Both;

			_camera.RenderToCubemap(_cubemap, 63, Camera.MonoOrStereoscopicEye.Left);
			SetViewport(new Vector2(0.5f, 1.0f), new Vector2(-0.5f, 0.0f));
			Graphics.Blit(_cubemap, RenderTarget, _material);

			_camera.RenderToCubemap(_cubemap, 63, Camera.MonoOrStereoscopicEye.Right);
			SetViewport(new Vector2(0.5f, 1.0f), new Vector2(0.5f, 0.0f));
			Graphics.Blit(_cubemap, RenderTarget, _material);

			_camera.stereoSeparation = tmpStereoSepration;
			_camera.stereoTargetEye = tmpStereoTargetEye;
		}
		else
		{
			_camera.RenderToCubemap(_cubemap);
			SetViewport(new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f));
			Graphics.Blit(_cubemap, RenderTarget, _material);
		}
	}

	private void OnDestroy()
	{
		if (_material != null)
		{
			Destroy(_material);
			_material = null;
		}

		if (_cubemap != null)
		{
			Destroy(_cubemap);
			_cubemap = null;
		}
	}

	private void SetViewport(Vector2 scale, Vector2 offset)
	{
		_material.SetVector("_ScaleOffset", new Vector4(scale.x, scale.y, offset.x, offset.y));
	}

	private void SetEnableKeyword(Material material, string keyword, bool flag)
	{
		if (flag)
		{
			material.EnableKeyword(keyword);
		}
		else
		{
			material.DisableKeyword(keyword);
		}
	}
}
