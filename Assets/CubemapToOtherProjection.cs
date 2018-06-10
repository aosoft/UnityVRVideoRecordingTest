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
	FishEye_Circumference,
	FishEye_Diagonal
}

public enum FishEyeType
{
	Equidistance,
	EquisolidAngle,
	Orthogonal
}

[RequireComponent(typeof(Camera))]
public class CubemapToOtherProjection : MonoBehaviour
{
	public RenderTexture RenderTarget;
	public int CubemapSize = 1024;
	public ProjectionType ProjectionType = ProjectionType.Equirectangular_360;
	public FishEyeType FishEyeType = FishEyeType.Equidistance;
	public bool UseUnityInternalCubemapRenderer = false;
	public GammaConvertType GammaConvertType = GammaConvertType.None;
	public bool RenderInStereo = false;
	public float StereoSeparation = 0.065f;
	public bool CorrectCameraPositionInStereoRendering = false;

	private Camera _camera;
	private Material _material;
	private RenderTexture _cubemap;
	private CubemapRenderer _cubemapRenderer;

	// Use this for initialization
	void Start()
	{
		_camera = GetComponent<Camera>();

		_material = new Material(Shader.Find("Unlit/CubemapToOtherProjection"));
		_cubemap = new RenderTexture(CubemapSize, CubemapSize, 24, RenderTextureFormat.ARGB32);
		_cubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;

		if (!UseUnityInternalCubemapRenderer)
		{
			_cubemapRenderer = new CubemapRenderer(CubemapSize);
		}
	}

	private void LateUpdate()
	{
		if (RenderTarget != null)
		{
			//StartCoroutine(InternalUpdateAsync());
			InternalUpdate();
		}
	}

	IEnumerator InternalUpdateAsync()
	{
		yield return new WaitForEndOfFrame();
		InternalUpdate();
	}

	// Update is called once per frame
	void InternalUpdate()
	{
		if (RenderTarget == null)
		{
			return;
		}

		switch (ProjectionType)
		{
			case ProjectionType.Equirectangular_360:
				{
					SetUVScaleOffset(Mathf.PI * 2.0f, Mathf.PI, 0.0f, 0.0f); 
					SetEnableProjFishEye(false);
					SetEnableEquiSolidAngle(false);
					SetEnableOrthogal(false);
				}
				break;

			case ProjectionType.Equirectangular_180:
				{
					SetUVScaleOffset(Mathf.PI, Mathf.PI, Mathf.PI * 0.5f, 0.0f);
					SetEnableProjFishEye(false);
					SetEnableEquiSolidAngle(false);
					SetEnableOrthogal(false);
				}
				break;

			case ProjectionType.FishEye_Circumference:
				{
					SetUVScaleOffset(2.0f, 2.0f, -1.0f, -1.0f);
					SetEnableProjFishEye(true);
					SetFishEyeDiameterScale(1.0f);
				}
				break;

			case ProjectionType.FishEye_Diagonal:
				{
					SetUVScaleOffset(2.0f, 2.0f, -1.0f, -1.0f);
					SetEnableProjFishEye(true);
					SetFishEyeDiameterScale(1.0f / Mathf.Sqrt(2));
				}
				break;
		}

		switch (FishEyeType)
		{
			case FishEyeType.Equidistance:
				{
					SetEnableEquiSolidAngle(false);
					SetEnableOrthogal(false);
				}
				break;

			case FishEyeType.EquisolidAngle:
				{
					SetEnableEquiSolidAngle(true);
					SetEnableOrthogal(false);
				}
				break;

			case FishEyeType.Orthogonal:
				{
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
				RenderToPanoramaView(ProjectionType, Camera.MonoOrStereoscopicEye.Left, 1.0f, 0.5f, 0.0f, -0.5f);
				RenderToPanoramaView(ProjectionType, Camera.MonoOrStereoscopicEye.Right, 1.0f, 0.5f, 0.0f, 0.5f);
			}
			else
			{
				RenderToPanoramaView(ProjectionType, Camera.MonoOrStereoscopicEye.Left, 0.5f, 1.0f, -0.5f, 0.0f);
				RenderToPanoramaView(ProjectionType, Camera.MonoOrStereoscopicEye.Right, 0.5f, 1.0f, 0.5f, 0.0f);
			}

			_camera.stereoSeparation = tmpStereoSepration;
			_camera.stereoTargetEye = tmpStereoTargetEye;
		}
		else
		{
			RenderToPanoramaView(ProjectionType, Camera.MonoOrStereoscopicEye.Mono, 1.0f, 1.0f, 0.0f, 0.0f);

			//_cubemapRenderer.RenderCubemap(_camera, 0.1f);
			//Graphics.Blit(_cubemapRenderer.Cubemap, RenderTarget, _material);
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

		if (_cubemapRenderer != null)
		{
			_cubemapRenderer.Dispose();
			_cubemapRenderer = null;
		}
	}

	private void RenderToPanoramaView(ProjectionType projectionType, Camera.MonoOrStereoscopicEye eye, float scaleX, float scaleY, float offsetX, float offsetY)
	{
		if (UseUnityInternalCubemapRenderer)
		{
			switch (GammaConvertType)
			{
				case GammaConvertType.Linear_to_sRGB:
					{
						SetEnableLinearToSRGB(true);
						SetEnableLinearToBT709(false);
					}
					break;

				case GammaConvertType.Linear_to_BT709:
					{
						SetEnableLinearToSRGB(false);
						SetEnableLinearToBT709(true);
					}
					break;

				default:
					{
						SetEnableLinearToSRGB(false);
						SetEnableLinearToBT709(false);
					}
					break;
			}

			_camera.RenderToCubemap(_cubemap, 63, eye);

			SetPositionScaleOffset(scaleX, scaleY, offsetX, offsetY);
			var q = Quaternion.identity;
			var t = _camera.transform;
			while (t != null)
			{
				q = q * t.localRotation;
				t = t.parent;
			}
			_material.SetMatrix("_Matrix", Matrix4x4.Rotate(q));
			Graphics.Blit(_cubemap, RenderTarget, _material);
		}
		else
		{
			float ipdOffset = 0.0f;
			switch (eye)
			{
				case Camera.MonoOrStereoscopicEye.Left:
					{
						ipdOffset = -StereoSeparation / 2.0f;
					}
					break;

				case Camera.MonoOrStereoscopicEye.Right:
					{
						ipdOffset = StereoSeparation / 2.0f;
					}
					break;
			}
			_cubemapRenderer.RenderCubemap(
				_camera,
				projectionType == ProjectionType.Equirectangular_360 ? 63 : 63 - (1 << (int)CubemapFace.NegativeZ),
				ipdOffset,
				GammaConvertType,
				CorrectCameraPositionInStereoRendering);

			SetPositionScaleOffset(scaleX, scaleY, offsetX, offsetY);
			_material.SetMatrix("_Matrix", Matrix4x4.identity);
			SetEnableLinearToSRGB(false);
			SetEnableLinearToBT709(false);
			Graphics.Blit(_cubemapRenderer.Cubemap, RenderTarget, _material);
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

	private void SetEnableLinearToSRGB(bool flag)
	{
		SetEnableKeyword("LINEAR_TO_SRGB", flag);
	}

	private void SetEnableLinearToBT709(bool flag)
	{
		SetEnableKeyword("LINEAR_TO_BT709", flag);
	}

	private void SetPositionScaleOffset(float scaleX, float scaleY, float offsetX, float offsetY)
	{
		_material.SetVector("_PositionScaleOffset", new Vector4(scaleX, scaleY, offsetX, offsetY));
	}

	private void SetUVScaleOffset(float scaleX, float scaleY, float offsetX, float offsetY)
	{
		_material.SetVector("_UVScaleOffset", new Vector4(scaleX, scaleY, offsetX, offsetY));
	}

	private void SetFishEyeDiameterScale(float scale)
	{
		_material.SetFloat("_FishEyeDiameterScale", scale);
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
