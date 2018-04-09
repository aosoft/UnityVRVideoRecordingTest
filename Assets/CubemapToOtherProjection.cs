using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StereoType
{
	None,
	LeftRight,
	TopDown
}

public enum ProjectionType
{
	Equirectangular_360 = 0,
	Equirectangular_180,
	FishEye_Equidistance,
	FishEye_EquisolidAngle,
	FishEye_Orthogonal
}

[RequireComponent(typeof(Camera))]
public class CubemapToOtherProjection : MonoBehaviour
{
	public RenderTexture RenderTarget;
	public int CubemapSize = 1024;
	public ProjectionType ProjectionType = ProjectionType.Equirectangular_360;
	public bool RenderInStereo = false;
	public float StereoSeparation = 0.065f;


	private Camera _camera;
	private Material _material;
	private RenderTexture _cubemap;

	// Use this for initialization
	void Start()
	{
		_camera = GetComponent<Camera>();

		_material = new Material(Shader.Find("Conversion/CubemapToOtherProjection"));
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

		switch (ProjectionType)
		{
			case ProjectionType.Equirectangular_360:
				{
					SetUVScaleOffset(Mathf.PI * 2.0f, Mathf.PI, 0.0f, 0.0f); 
					SetEnableProjFishEye(false);
					SetEnableEquiSolidAngle(false);
					SetEnableOrthogal(false);
					SetEquirectangularScale(2.0f);
				}
				break;

			case ProjectionType.Equirectangular_180:
				{
					SetUVScaleOffset(Mathf.PI, Mathf.PI, Mathf.PI * 0.5f, 0.0f);
					SetEnableProjFishEye(false);
					SetEnableEquiSolidAngle(false);
					SetEnableOrthogal(false);
					SetEquirectangularScale(1.0f);
				}
				break;

			case ProjectionType.FishEye_Equidistance:
				{
					SetUVScaleOffset(2.0f, 2.0f, -1.0f, -1.0f);
					SetEnableProjFishEye(true);
					SetEnableEquiSolidAngle(false);
					SetEnableOrthogal(false);
				}
				break;

			case ProjectionType.FishEye_EquisolidAngle:
				{
					SetUVScaleOffset(2.0f, 2.0f, -1.0f, -1.0f);
					SetEnableProjFishEye(true);
					SetEnableEquiSolidAngle(true);
					SetEnableOrthogal(false);
				}
				break;

			case ProjectionType.FishEye_Orthogonal:
				{
					SetUVScaleOffset(2.0f, 2.0f, -1.0f, -1.0f);
					SetEnableProjFishEye(true);
					SetEnableEquiSolidAngle(false);
					SetEnableOrthogal(true);
				}
				break;
		}


		if (RenderInStereo)
		{
			var tmpStereoSepration = _camera.stereoSeparation;
			var tmpStereoTargetEye = _camera.stereoTargetEye;
			_camera.stereoSeparation = StereoSeparation;
			_camera.stereoTargetEye = StereoTargetEyeMask.Both;

			if (ProjectionType == ProjectionType.Equirectangular_360)
			{
				_camera.RenderToCubemap(_cubemap, 63, Camera.MonoOrStereoscopicEye.Left);
				SetPositionScaleOffset(1.0f, 0.5f, 0.0f, -0.5f);
				Graphics.Blit(_cubemap, RenderTarget, _material);

				_camera.RenderToCubemap(_cubemap, 63, Camera.MonoOrStereoscopicEye.Right);
				SetPositionScaleOffset(1.0f, 0.5f, 0.0f, 0.5f);
				Graphics.Blit(_cubemap, RenderTarget, _material);
			}
			else
			{
				_camera.RenderToCubemap(_cubemap, 63, Camera.MonoOrStereoscopicEye.Left);
				SetPositionScaleOffset(0.5f, 1.0f, -0.5f, 0.0f);
				Graphics.Blit(_cubemap, RenderTarget, _material);

				_camera.RenderToCubemap(_cubemap, 63, Camera.MonoOrStereoscopicEye.Right);
				SetPositionScaleOffset(0.5f, 1.0f, 0.5f, 0.0f);
				Graphics.Blit(_cubemap, RenderTarget, _material);
			}


			_camera.stereoSeparation = tmpStereoSepration;
			_camera.stereoTargetEye = tmpStereoTargetEye;
		}
		else
		{
			_camera.RenderToCubemap(_cubemap);
			SetPositionScaleOffset(1.0f, 1.0f, 0.0f, 0.0f);
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

	private void SetEnableProjFishEye(bool flag)
	{
		SetEnableKeyword("PROJ_FISHEYE", flag);
	}

	private void SetEnableEquiSolidAngle(bool flag)
	{
		SetEnableKeyword("ANGLEFUNC_EQUISOLIDANGLE", flag);
	}

	private void SetEnableOrthogal(bool flag)
	{
		SetEnableKeyword("ANGLEFUNC_ORTHGONAL", flag);
	}

	private void SetPositionScaleOffset(float scaleX, float scaleY, float offsetX, float offsetY)
	{
		_material.SetVector("_PositionScaleOffset", new Vector4(scaleX, scaleY, offsetX, offsetY));
	}

	private void SetUVScaleOffset(float scaleX, float scaleY, float offsetX, float offsetY)
	{
		_material.SetVector("_UVScaleOffset", new Vector4(scaleX, scaleY, offsetX, offsetY));
	}

	private void SetEquirectangularScale(float scale)
	{
		_material.SetFloat("_EquirectangularScale", scale);
	}

	private void SetEnableKeyword(string keyword, bool flag)
	{
		if (flag)
		{
			_material.EnableKeyword(keyword);
		}
		else
		{
			_material.DisableKeyword(keyword);
		}
	}
}
