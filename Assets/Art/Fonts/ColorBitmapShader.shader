Shader "Custom/TMPBitmapCustomColor"
{
    Properties
    {
        _MainTex ("Font Texture", 2D) = "white" {}
        _FaceColor ("Text Color", Color) = (1,1,1,1)
        _Speed ("Plasma Speed", Range(0.1, 10)) = 3.0
        _Scale ("Plasma Scale", Range(1, 50)) = 15.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }

        Cull Off
        ZWrite Off
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _FaceColor;
            float _Speed;
            float _Scale;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _FaceColor;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.texcoord);
                float alpha = texColor.a * i.color.a;

                // Klassiek plasma-effect op basis van UV en _Time.y
                float2 uv = i.texcoord * _Scale;
                float t = _Time.y * _Speed;

                float v1 = sin(uv.x + t);
                float v2 = sin(uv.y + t);
                float v3 = sin(uv.x + uv.y + t);
                float plasmaValue = (v1 + v2 + v3 + 3.0) / 6.0;

                // Lava kleurovergang van rood naar geel
                fixed3 redColor = fixed3(1.0, 0.1, 0.0);
                fixed3 yellowColor = fixed3(1.0, 0.9, 0.0);
                fixed3 plasmaColor = lerp(redColor, yellowColor, plasmaValue);

                fixed4 finalColor = fixed4(plasmaColor * i.color.rgb, alpha);
                return finalColor;
            }
            ENDCG
        }
    }
}