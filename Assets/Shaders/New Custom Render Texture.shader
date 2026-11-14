Shader "Custom/TransparentRimDoubleSided"
{
    Properties
    {
        _Color      ("Base Color (A = прозорість)", Color) = (0.2, 0.6, 1, 0.15)
        _RimColor   ("Rim Color", Color) = (0.4, 0.9, 1, 1)
        _RimPower   ("Rim Power", Range(0.5, 8)) = 3
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 1.5
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    fixed4 _Color;
    fixed4 _RimColor;
    float  _RimPower;
    float  _RimIntensity;

    struct appdata
    {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
    };

    struct v2f
    {
        float4 pos     : SV_POSITION;
        float3 normalW : TEXCOORD0;
        float3 viewDir : TEXCOORD1;
    };

    v2f vert (appdata v)
    {
        v2f o;
        o.pos     = UnityObjectToClipPos(v.vertex);
        o.normalW = UnityObjectToWorldNormal(v.normal);
        float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
        o.viewDir = _WorldSpaceCameraPos - worldPos;
        return o;
    }

    fixed4 frag (v2f i) : SV_Target
    {
        float3 n = normalize(i.normalW);
        float3 v = normalize(i.viewDir);

        // abs() робить ефект однаковим і для зовнішньої, і для внутрішньої сторони
        float ndv  = abs(dot(n, v));
        float rim  = pow(1.0 - saturate(ndv), _RimPower) * _RimIntensity;

        fixed3 col = _Color.rgb + _RimColor.rgb * rim;
        float  a   = saturate(_Color.a + rim * _RimColor.a);

        return fixed4(col, a);
    }
    ENDCG

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        // Прохід 1: спочатку задні грані (внутрішня сторона)
        Pass
        {
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }

        // Прохід 2: потім передні грані (зовнішня сторона)
        Pass
        {
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
}