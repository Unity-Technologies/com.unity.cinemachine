Shader "Custom/FadeOutAngle" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _MinDistance ("Minimum Distance", Float) = 2
        _MaxDistance ("Maximum Distance", Float) = 3
        _MinAngle ("Min Angle (radian)", Float) = 0.3
        _MaxAngle ("Max Angle (radian)", Float) = 0.3
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
 
    Pass {
        ZWrite On
        ColorMask 0
    }
   
        CGPROGRAM
        #pragma surface surf Lambert alpha:fade
        #pragma target 3.0
        sampler2D _MainTex;
        struct Input {
            float2 uv_MainTex;
            float3 worldPos;
        };
        half _Glossiness;
        half _Metallic;
        float _MinDistance;
        float _MaxDistance;
        float _MinAngle;
        float _MaxAngle;
        fixed4 _Color;
        void surf (Input IN, inout SurfaceOutput o) {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            
            float distanceFromCamera = distance(IN.worldPos, _WorldSpaceCameraPos);
            float fade = 1;
            if (_MinDistance < distanceFromCamera && distanceFromCamera < _MaxDistance) {
                float3 cameraForward = mul((float3x3)unity_CameraToWorld, float3(0,0,1));
                float3 cameraUp = mul((float3x3)unity_CameraToWorld, float3(0,1,0));
                float3 cameraToObject = IN.worldPos - _WorldSpaceCameraPos;
                float3 cameraToObject_ProjPlane = cameraToObject - cameraUp * dot(cameraToObject, cameraUp);
                float angle = acos(dot(normalize(cameraForward), normalize(cameraToObject_ProjPlane)));
                
                fade = saturate((angle - _MinAngle) / _MaxAngle);
            }
            o.Alpha = c.a * fade;
            
            // help: https://developer.download.nvidia.com/cg/index_stdlib.html
        }
        ENDCG
    }
    FallBack "Diffuse"
}