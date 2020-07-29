// Simplified version of the SDF Surface shader :
// - No support for Bevel, Bump or envmap
// - Diffuse only lighting
// - Fully supports only 1 directional light. Other lights can affect it, but it will be per-vertex/SH.

Shader "TextMeshPro/Mobile/Distance Field Simple" {

Properties { 
  _FaceTex		    ("Fill Texture", 2D) = "white" {}
	_FaceColor			("Fill Color", Color) = (1,1,1,1)
	_FaceDilate			("Face Dilate", Range(-1,1)) = 0

  _FaceUVSpeedX   ("Face UV Speed X", Range(-5, 5)) = 0.0
  _FaceUVSpeedY   ("Face UV Speed Y", Range(-5, 5)) = 0.0

  _OutlineUVSpeedX   ("Face UV Speed X", Range(-5, 5)) = 0
  _OutlineUVSpeedY   ("Face UV Speed Y", Range(-5, 5)) = 0

	_OutlineColor		  ("Outline Color", Color) = (0,0,0,1)
	_OutlineTex			  ("Outline Texture", 2D) = "white" {}
	_OutlineWidth	    ("Outline Thickness", Range(0, 1)) = 0
	_OutlineSoftness	("Outline Softness", Range(0,1)) = 0

  _UnderlayColor		("Border Color", Color) = (0,0,0,.5)
	_UnderlayOffsetX 	("Border OffsetX", Range(-1,1)) = 0
	_UnderlayOffsetY 	("Border OffsetY", Range(-1,1)) = 0
	_UnderlayDilate		("Border Dilate", Range(-1,1)) = 0
	_UnderlaySoftness 	("Border Softness", Range(0,1)) = 0 


	_GlowColor			("Color", Color) = (0, 1, 0, 0.5)
	_GlowOffset			("Offset", Range(-1,1)) = 0
	_GlowInner			("Inner", Range(0,1)) = 0.05
	_GlowOuter			("Outer", Range(0,1)) = 0.05
	_GlowPower			("Falloff", Range(1, 0)) = 0.75

	_WeightNormal		("Weight Normal", float) = 0
	_WeightBold			("Weight Bold", float) = 0.5

	// Should not be directly exposed to the user
	_ShaderFlags		("Flags", float) = 0
	_ScaleRatioA		("Scale RatioA", float) = 1
	_ScaleRatioB		("Scale RatioB", float) = 1
	_ScaleRatioC		("Scale RatioC", float) = 1

	_MainTex			      ("Font Atlas", 2D) = "white" {}
	_TextureWidth		    ("Texture Width", float) = 1024
	_TextureHeight		  ("Texture Height", float) = 1024
	_GradientScale		  ("Gradient Scale", float) = 5.0
	_ScaleX				      ("Scale X", float) = 1.0
	_ScaleY				      ("Scale Y", float) = 1.0
	_PerspectiveFilter	("Perspective Correction", Range(0, 1)) = 0.875

	_VertexOffsetX		("Vertex OffsetX", float) = 0
	_VertexOffsetY		("Vertex OffsetY", float) = 0
	
	//_MaskCoord		  ("Mask Coords", vector) = (0,0,0,0)
	//_MaskSoftness		("Mask Softness", float) = 0

	_StencilComp		  ("Stencil Comparison", Float) = 8
	_Stencil			    ("Stencil ID", Float) = 0
	_StencilOp			  ("Stencil Operation", Float) = 0
	_StencilWriteMask	("Stencil Write Mask", Float) = 255
	_StencilReadMask	("Stencil Read Mask", Float) = 255
	
	_ColorMask			  ("Color Mask", Float) = 15

	_ClipRect("Clip Rect", vector) = (-32767, -32767, 32767, 32767)

    _TestValue      ("Test Value", Range(0,1)) = 0.0

}

SubShader {
    Tags
    {
      "Queue"="Transparent"
      "IgnoreProjector"="True"
      "RenderType"="Transparent"
    }
    Stencil
    {
      Ref [_Stencil]
      Comp [_StencilComp]
      Pass [_StencilOp] 
      ReadMask [_StencilReadMask]
      WriteMask [_StencilWriteMask]
    }

    Cull [_CullMode]
    ZWrite Off
    //ZWrite On
    Lighting Off
    Fog { Mode Off }
    ZTest [unity_GUIZTestMode]
    //Blend One OneMinusSrcAlpha
    Blend SrcAlpha OneMinusSrcAlpha
    //ColorMask [_ColorMask]

    //LOD 300

  Pass {
    // UNDERLAY PASS
  	CGPROGRAM
    #pragma vertex VertShader
    #pragma fragment PixShader
  	#pragma target 2.0
    #pragma shader_feature __ UNDERLAY_ON
    //#pragma shader_feature __ OUTLINE_ON    // FOR TESTING

  
    #include "UnityCG.cginc"
  	#include "TMPro_Properties.cginc"
  	#include "TMPro.cginc"
  
		struct appdata {
			float4 vertex : POSITION;
			fixed4 color : COLOR;
			float2 texcoord1 : TEXCOORD0;
      float2 texcoord2 : TEXCOORD1;
			float3 normal : NORMAL;
		};

  	struct v2f
  	{
      float4  vertex : SV_Position;
  		fixed4	color		: COLOR;
  		float2	uv1 : TEXCOORD0;
  		float4  uv2 : TEXCOORD1;
      #if UNDERLAY_ON
  		  float4  uParam : TEXCOORD2;
      #endif
  	};

    float4 _MainTex_ST;

    v2f VertShader(appdata v)
    {
    	//v.vertex.x += _VertexOffsetX;
    	//v.vertex.y += _VertexOffsetY;
    
      v2f data;
        data.color = v.color;
        data.uv1.xy = TRANSFORM_TEX(v.texcoord1, _MainTex);

      float bold = step(v.texcoord2.y, 0);

      float4 vert = v.vertex;
      float4 vPosition = UnityObjectToClipPos(vert);
      float2 pixelSize = vPosition.w;

      pixelSize /= float2(_ScaleX, _ScaleY) * mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy);
      float scale = rsqrt(dot(pixelSize, pixelSize));
      scale *= abs(v.texcoord2.y) * _GradientScale * 1.5;
      scale = lerp(scale * (1 - _PerspectiveFilter), scale, abs(dot(UnityObjectToWorldNormal(v.normal.xyz), normalize(WorldSpaceViewDir(vert)))));

      data.uv2.w = scale;
      data.uv2.z = (lerp(_WeightNormal, _WeightBold, bold) / 4.0 + _FaceDilate) * _ScaleRatioA * 0.5;

      #if UNDERLAY_ON
        float layerScale = data.uv2.w;
        layerScale /= 1 + ((_UnderlaySoftness * _ScaleRatioC) * layerScale);
        float layerBias = (.5 - data.uv2.z) * layerScale - .5 - ((_UnderlayDilate * _ScaleRatioC) * .5 * layerScale);

        float x = -(_UnderlayOffsetX * _ScaleRatioC) * _GradientScale / _TextureWidth;
        float y = -(_UnderlayOffsetY * _ScaleRatioC) * _GradientScale / _TextureHeight;

        data.uParam = float4(data.uv1.x+x, data.uv1.y+y, layerScale, layerBias);
      #endif

      data.vertex = vPosition;

      return data;
    }
  
    fixed4 PixShader(v2f input) : SV_Target
    {
      	float scale = input.uv2.w;
      
      	// Signed distance
      	float c = tex2D(_MainTex, input.uv1.xy).a;
      	float sd = (.5 - c - input.uv2.z) * scale + .5;
      	float outline = _OutlineWidth*_ScaleRatioA * scale;
      	float softness = _OutlineSoftness*_ScaleRatioA * scale;
      
      	// Color & Alpha
      	float4 outlineColor = _OutlineColor;
      	outlineColor.a *= input.color.a;

      	float4 faceColor = _FaceColor;

        faceColor.a = 0;
        outlineColor.a = 0;
      	faceColor = GetColor(sd, faceColor, outlineColor, outline, softness);
      	faceColor.rgb /= max(faceColor.a, 0.0001);

        #if UNDERLAY_ON
          half d = tex2D(_MainTex, input.uParam.xy).a * input.uParam.z;
          faceColor.rgb = _UnderlayColor.rgb;
          faceColor.a = _UnderlayColor.a * saturate(d - input.uParam.w) * (1 - faceColor.a) * input.color.a;
        #endif

        return faceColor;
      }
  
  	  ENDCG
     }

  Pass {  
    // OUTLINE PASS //
  	CGPROGRAM
    #pragma vertex VertShader
    #pragma fragment PixShader
  	#pragma target 2.0
    //#pragma shader_feature __ OUTLINE_ON    // FOR TESTING
  
    #include "UnityCG.cginc"
  	#include "TMPro_Properties.cginc"
  	#include "TMPro.cginc"
  
		struct appdata {
			float4 vertex : POSITION;
			fixed4 color : COLOR;
			float2 texcoord1 : TEXCOORD0;
      float2 texcoord2 : TEXCOORD1;
			float3 normal : NORMAL;
		};

  	struct v2f
  	{
      float4  vertex : SV_Position;
  		fixed4	color		: COLOR;
  		float4	uv1 : TEXCOORD0;
  		float4  uv2 : TEXCOORD1;
  	};

    float4 _MainTex_ST;
    float4 _FaceTex_ST;
    float4 _OutlineTex_ST;

    v2f VertShader(appdata v)
    {
      v.vertex.x += _VertexOffsetX;
      v.vertex.y += _VertexOffsetY;
    
      v2f data;
        data.color = v.color;
        data.uv1.xy = TRANSFORM_TEX(v.texcoord1, _MainTex);
        data.uv1.zw = TRANSFORM_TEX(UnpackUV(v.texcoord2), _FaceTex);
        data.uv2.xy = TRANSFORM_TEX(UnpackUV(v.texcoord2), _OutlineTex);

      float bold = step(v.texcoord2.y, 0);

      float4 vert = v.vertex;
      float4 vPosition = UnityObjectToClipPos(vert);
      float2 pixelSize = vPosition.w;

      pixelSize /= float2(_ScaleX, _ScaleY) * mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy);
      float scale = rsqrt(dot(pixelSize, pixelSize));
      scale *= abs(v.texcoord2.y) * _GradientScale * 1.5;
      scale = lerp(scale * (1 - _PerspectiveFilter), scale, abs(dot(UnityObjectToWorldNormal(v.normal.xyz), normalize(WorldSpaceViewDir(vert)))));

      data.uv2.w = scale;
      data.uv2.z = (lerp(_WeightNormal, _WeightBold, bold) / 4.0 + _FaceDilate) * _ScaleRatioA * 0.5;
       
      data.vertex = vPosition;

      return data;
    }
  
    fixed4 PixShader(v2f input) : SV_Target
    {
      	float scale = input.uv2.w;
      
      	// Signed distance
      	float c = tex2D(_MainTex, input.uv1.xy).a;
      	float sd = (.5 - c - input.uv2.z) * scale + .5;
      	float outline = _OutlineWidth*_ScaleRatioA * scale;
      	float softness = _OutlineSoftness*_ScaleRatioA * scale;
      
      	// Color & Alpha
      	float4 outlineColor = _OutlineColor;
      	outlineColor.a *= input.color.a;
      	outlineColor *= tex2D(_OutlineTex, float2(input.uv2.x + _OutlineUVSpeedX * _Time.y, input.uv2.y + _OutlineUVSpeedY * _Time.y));

      	float4 faceColor = _FaceColor * input.color;

        //#if OUTLINE_ON  // FOR TESTING
          outlineColor.a = outlineColor.a;
        //#else
        //  outlineColor.a = 0;
        //#endif

        //faceColor.a = 0;
      	faceColor = GetColor(sd, faceColor, outlineColor, outline, softness);
      	faceColor.rgb /= max(faceColor.a, 0.0001);

        return faceColor;
      }
  
  	  ENDCG
     }
  
  Pass {
    // FACE PASS
  	CGPROGRAM
    #pragma vertex VertShader
    #pragma fragment PixShader
  	#pragma target 2.0
    #pragma shader_feature __ GLOW_ON
    //#pragma shader_feature __ OUTLINE_ON    // FOR TESTING

  
    #include "UnityCG.cginc"
  	#include "TMPro_Properties.cginc"
  	#include "TMPro.cginc"
  
		struct appdata {
			float4 vertex : POSITION;
			fixed4 color : COLOR;
			float2 texcoord1 : TEXCOORD0;
      float2 texcoord2 : TEXCOORD1;
			float3 normal : NORMAL;
		};

  	struct v2f
  	{
      float4  vertex : SV_Position;
  		fixed4	color		: COLOR;
  		float4	uv1 : TEXCOORD0;
  		float4  uv2 : TEXCOORD1;
  	};

    float4 _MainTex_ST;
    float4 _FaceTex_ST;
    float4 _OutlineTex_ST;

    v2f VertShader(appdata v)
    {
    	v.vertex.x += _VertexOffsetX;
    	v.vertex.y += _VertexOffsetY;
    
      v2f data;
        data.color = v.color;
        data.uv1.xy = TRANSFORM_TEX(v.texcoord1, _MainTex);
        data.uv1.zw = TRANSFORM_TEX(UnpackUV(v.texcoord2), _FaceTex);
        data.uv2.xy = TRANSFORM_TEX(UnpackUV(v.texcoord2), _OutlineTex);

    	float bold = step(v.texcoord2.y, 0);

      float4 vert = v.vertex;
      float4 vPosition = UnityObjectToClipPos(vert);
      float2 pixelSize = vPosition.w;

      pixelSize /= float2(_ScaleX, _ScaleY) * mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy);
      float scale = rsqrt(dot(pixelSize, pixelSize));
      scale *= abs(v.texcoord2.y) * _GradientScale * 1.5;
      scale = lerp(scale * (1 - _PerspectiveFilter), scale, abs(dot(UnityObjectToWorldNormal(v.normal.xyz), normalize(WorldSpaceViewDir(vert)))));

      data.uv2.w = scale;
      //data.uv2.w = 32;
    	data.uv2.z = (lerp(_WeightNormal, _WeightBold, bold) / 4.0 + _FaceDilate) * _ScaleRatioA * 0.5;
       
      data.vertex = vPosition;

      return data;
    }
  
    fixed4 PixShader(v2f input) : SV_Target
    {
      	float scale = input.uv2.w;
      
      	// Signed distance
      	float c = tex2D(_MainTex, input.uv1.xy).a;
      	float sd = (.5 - c - input.uv2.z) * scale + .5;
      	float outline = _OutlineWidth*_ScaleRatioA * scale;
      	float softness = _OutlineSoftness*_ScaleRatioA * scale;
      
      	// Color & Alpha
      	float4 outlineColor = _OutlineColor;
      	float4 faceColor = _FaceColor;
      	faceColor *= input.color;
        faceColor *= tex2D(_FaceTex, float2(input.uv1.z + _FaceUVSpeedX * _Time.y, input.uv1.w + _FaceUVSpeedY * _Time.y));

        outlineColor.a = 0;
      	faceColor = GetColor(sd, faceColor, outlineColor, outline, softness);
      	faceColor.rgb /= max(faceColor.a, 0.0001);
      
        #if GLOW_ON
          float4 glowColor = GetGlowColor(sd, scale);
          glowColor.a *= input.color.a;
          faceColor = BlendARGB(glowColor, faceColor);
          faceColor.rgb /= max(faceColor.a, 0.0001);
        #endif

        return faceColor;
      }
  
  	  ENDCG
     }
  }
  CustomEditor "TMPro.EditorUtilities.TMP_SDFShaderGUI"
}


