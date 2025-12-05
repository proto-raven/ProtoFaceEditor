Shader "Toast/PixelGridOverlay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GridColorLight ("Light Grid Color", Color) = (1,1,1,0.35)
        _GridColorDark  ("Dark Grid Color",  Color) = (0,0,0,0.5)
        _GridResolution ("Grid Resolution", Vector) = (128,32,0,0)
        _Thickness      ("Line Thickness",  Float)  = 0.06
        _GridEnabled    ("Grid Enabled",    Float)  = 1

        // --- REQUIRED FOR UI MASKING (RectMask2D / Mask) ---
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        // --- This stencil block makes the shader respect UI masks ---
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        ColorMask [_ColorMask]

        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _GridColorLight;
            float4 _GridColorDark;
            float4 _GridResolution;   // (width, height, 0, 0)
            float  _Thickness;        // cell-edge thickness in [0..0.5]
            float  _GridEnabled;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // If grid disabled, be fully transparent
                if (_GridEnabled <= 0.5)
                    return fixed4(0,0,0,0);

                // Underlying frame texel
                fixed4 baseCol = tex2D(_MainTex, i.uv);

                // Convert UV to grid space (0..gridSize)
                float2 gridCoord = i.uv * _GridResolution.xy;

                // Fractional position inside the current cell (0..1)
                float2 cell = frac(gridCoord);

                // Distance to nearest edge of the cell
                float2 edgeDist = min(cell, 1.0 - cell);
                float distToEdge = min(edgeDist.x, edgeDist.y);

                // 1 where we are within _Thickness of an edge, else 0
                float lineMask = step(distToEdge, _Thickness);
                if (lineMask < 0.5)
                    return fixed4(0,0,0,0); // no grid here

                // Perceived brightness of underlying pixel
                float brightness = dot(baseCol.rgb, float3(0.299, 0.587, 0.114));
                if (baseCol.a < 0.01)
                    brightness = 0.0; // transparent treated as dark

                float useDark = step(0.5, brightness); // 1 if bright
                float4 gridCol = lerp(_GridColorLight, _GridColorDark, useDark);

                return gridCol;
            }
            ENDCG
        }
    }
}
