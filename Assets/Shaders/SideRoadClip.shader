Shader "Drive And Dodge/Side Road Clip"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ClipX ("World Clip X", Float) = 0
        _ClipDirection ("Visible Side", Float) = -1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float worldX : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _ClipX;
            float _ClipDirection;

            v2f vert(appdata input)
            {
                v2f output;
                float4 worldPosition = mul(unity_ObjectToWorld, input.vertex);
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;
                output.worldX = worldPosition.x;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                clip(_ClipDirection * (input.worldX - _ClipX));
                return tex2D(_MainTex, input.uv) * input.color;
            }
            ENDCG
        }
    }
}
