using UnityEngine;
using UnityEngine.Rendering;

public enum GammaConvertType
{
	None = 0,
	Linear_to_sRGB,
	Linear_to_BT709
}

public class CubemapRenderer : System.IDisposable
{
	private CommandBuffer[] _commandBuffers = null;
	private Shader _shader = null;
	private Material _material = null;
	private Mesh _mesh = null;
	private int _cubemapSize = 0;
	private RenderTexture _cubemap = null;
	private RenderTexture _tempRT = null;

	struct FaceInfo
	{
		public Quaternion Rotate;
		public CubemapFace Face;
		public Vector3 PositionShift;

		public FaceInfo(Quaternion rotate, CubemapFace face, Vector3 positionShift)
		{
			Rotate = rotate;
			Face = face;
			PositionShift = positionShift;
		}
	}

	private static readonly FaceInfo[] _faces = new FaceInfo[]
	{
		new FaceInfo(Quaternion.Euler(0.0f, 90.0f, 0.0f), CubemapFace.PositiveX, new Vector3(0.0f, 1.0f, 0.0f)),
		new FaceInfo(Quaternion.Euler(0.0f, -90.0f, 0.0f), CubemapFace.NegativeX, new Vector3(0.0f, -1.0f, 0.0f)),
		new FaceInfo(Quaternion.Euler(-90.0f, 0.0f, 0.0f), CubemapFace.PositiveY, new Vector3(0.0f, 0.0f, 0.0f)),
		new FaceInfo(Quaternion.Euler(90.0f, 0.0f, 0.0f), CubemapFace.NegativeY, new Vector3(0.0f, 0.0f, 0.0f)),
		new FaceInfo(Quaternion.Euler(0.0f, 0.0f, 0.0f), CubemapFace.PositiveZ, new Vector3(1.0f, 0.0f, 0.0f)),
		new FaceInfo(Quaternion.Euler(0.0f, 180.0f, 0.0f), CubemapFace.NegativeZ, new Vector3(-1.0f, 0.0f, 0.0f)),
	};

	private static Vector3[] _meshVertices = new Vector3[]
	{
		new Vector3(1.0f, 1.0f, 0.0f),
		new Vector3(-1.0f, 1.0f, 0.0f),
		new Vector3(-1.0f, -1.0f, 0.0f),
		new Vector3(1.0f, -1.0f, 0.0f),
	};
	private static readonly int[] _meshIndices = new int[] { 0, 1, 2, 2, 3, 0 };


	public CubemapRenderer(int cubemapSize)
	{
		_cubemapSize = cubemapSize;

		_shader = Shader.Find("Unlit/CubemapRenderer");
		_material = new Material(_shader);


		_mesh = new Mesh
		{
			vertices = _meshVertices,
			triangles = _meshIndices
		};


		_cubemap = new RenderTexture(cubemapSize, cubemapSize, 0, RenderTextureFormat.ARGB32);
		_cubemap.dimension = TextureDimension.Cube;
		_cubemap.Create();

		var tid = Shader.PropertyToID("_MainTex");

		_commandBuffers = new CommandBuffer[_faces.Length];
		for (int i = 0; i < _faces.Length; i++)
		{
			var commandBuffer = new CommandBuffer();
			commandBuffer.SetGlobalTexture(tid, BuiltinRenderTextureType.CameraTarget);
			commandBuffer.SetRenderTarget(_cubemap, 0, _faces[i].Face);
			commandBuffer.DrawMesh(_mesh, Matrix4x4.identity, _material, 0, 0);
			_commandBuffers[i] = commandBuffer;
		}
	}

	public void Dispose()
	{
		if (_commandBuffers != null)
		{
			foreach (var c in _commandBuffers)
			{
				c.Dispose();
			}
			_commandBuffers = null;
		}

		if (_material != null)
		{
			Object.Destroy(_material);
			_material = null;
		}

		if (_mesh != null)
		{
			Object.Destroy(_mesh);
			_mesh = null;
		}

		if (_tempRT != null)
		{
			Object.Destroy(_tempRT);
			_tempRT = null;
		}

		if (_cubemap != null)
		{
			Object.Destroy(_cubemap);
			_cubemap = null;
		}

		if (_shader != null)
		{
			Object.Destroy(_shader);
			_shader = null;
		}
	}

	public void RenderCubemap(Camera camera, int faceMask, float ipdOffset, GammaConvertType gammaConvert, bool correctCameraPositionInStereoRendering)
	{
		if (_tempRT != null)
		{
			if (camera.allowHDR)
			{
				if (_tempRT.format != RenderTextureFormat.DefaultHDR)
				{
					Object.Destroy(_tempRT);
					_tempRT = null;
				}
			}
			else
			{
				if (_tempRT.format != RenderTextureFormat.Default)
				{
					Object.Destroy(_tempRT);
					_tempRT = null;
				}
			}
		}

		if (_tempRT == null)
		{
			_tempRT = new RenderTexture(_cubemapSize, _cubemapSize, 24,
				camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
			_tempRT.dimension = TextureDimension.Tex2D;
			_tempRT.Create();
		}

		switch (gammaConvert)
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

		var orgLocalRotation = camera.transform.localRotation;
		var orgLocalPosition = camera.transform.localPosition;
		var orgTargetTexture = camera.targetTexture;
		var orgOrthographic = camera.orthographic;
		var orgAspect = camera.aspect;
		var orgFieldOfView = camera.fieldOfView;

		camera.orthographic = false;
		camera.aspect = 1.0f;
		camera.fieldOfView = 90.0f;
		camera.targetTexture = _tempRT;
		try
		{
			for (int i = 0; i < _commandBuffers.Length; i++)
			{
				if ((faceMask & (1 << (int)_faces[i].Face)) == 0)
				{
					continue;
				}

				camera.AddCommandBuffer(CameraEvent.AfterEverything, _commandBuffers[i]);
				try
				{
					camera.transform.localRotation = orgLocalRotation * _faces[i].Rotate;
					camera.transform.localPosition =
						orgLocalPosition +
						(correctCameraPositionInStereoRendering ? _faces[i].PositionShift * ipdOffset : (new Vector3(1.0f, 0.0f, 0.0f) * ipdOffset));
					camera.Render();
				}
				finally
				{
					camera.RemoveCommandBuffer(CameraEvent.AfterEverything, _commandBuffers[i]);
				}
			}
		}
		finally
		{
			camera.transform.localRotation = orgLocalRotation;
			camera.transform.localPosition = orgLocalPosition;
			camera.targetTexture = orgTargetTexture;
			camera.orthographic = orgOrthographic;
			camera.aspect = orgAspect;
			camera.fieldOfView = orgFieldOfView;
		}
	}

	public RenderTexture Cubemap
	{
		get
		{
			return _cubemap;
		}
	}


	private void SetEnableLinearToSRGB(bool flag)
	{
		SetEnableKeyword("LINEAR_TO_SRGB", flag);
	}

	private void SetEnableLinearToBT709(bool flag)
	{
		SetEnableKeyword("LINEAR_TO_BT709", flag);
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

