using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CubemapToEquirectangular : MonoBehaviour
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

		_material = new Material(Shader.Find("Conversion/CubemapToEquirectangular"));
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

		if (RenderInStereo)
		{
			var tmpStereoSepration = _camera.stereoSeparation;
			var tmpStereoTargetEye = _camera.stereoTargetEye;
			_camera.stereoSeparation = StereoSeparation;
			_camera.stereoTargetEye = StereoTargetEyeMask.Both;

			_camera.RenderToCubemap(_cubemap, 63, Camera.MonoOrStereoscopicEye.Left);
			SetRenderTargetArea(true, false);
			Graphics.Blit(_cubemap, RenderTarget, _material);

			_camera.RenderToCubemap(_cubemap, 63, Camera.MonoOrStereoscopicEye.Right);
			SetRenderTargetArea(false, true);
			Graphics.Blit(_cubemap, RenderTarget, _material);

			_camera.stereoSeparation = tmpStereoSepration;
			_camera.stereoTargetEye = tmpStereoTargetEye;
		}
		else
		{
			_camera.RenderToCubemap(_cubemap);
			SetRenderTargetArea(false, false);
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

	private void SetRenderTargetArea(bool top, bool bottom)
	{
		SetEnableKeyword(_material, "RENDER_TOP", top);
		SetEnableKeyword(_material, "RENDER_BOTTOM", bottom);
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
