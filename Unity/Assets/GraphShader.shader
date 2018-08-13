// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/GraphShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "clear" {}
		_Darkness ("Darkness", Float) = .5
		//_Transparency ("Transparency", Float) = 1
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		LOD 1000

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			
			#include "UnityCG.cginc"

			struct appdata
			{
				fixed3 color : COLOR0;
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				float4 normal : NORMAL;
			};

			struct v2f
			{
				fixed3 color : COLOR0;
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				//float4 normal : NORMAL;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Darkness;
		
			v2f vert (appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);

				o.color.b = max(_Darkness, v.normal.x);
				o.color.r = max(_Darkness, v.normal.y);
				o.color.g = max(_Darkness, v.normal.z);// = v.normal * .5 + .5;
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				return fixed4(min(i.color.r, col.r), min(i.color.g, col.g), min(i.color.b, col.b), col.a);
			}
			ENDCG
		}
	}
}
