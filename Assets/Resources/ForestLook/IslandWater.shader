Shader "Hearthbound/Island Water"
{
 Properties { _Water("Water",2D)="white" {} }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma target 3.0
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_Water); SAMPLER(sampler_Water);
   struct A { float4 positionOS:POSITION; };
   struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; half fog:TEXCOORD1; };
   V Vert(A v)
   {
    V o; VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
    o.positionCS=p.positionCS;o.world=p.positionWS;o.fog=ComputeFogFactor(p.positionCS.z);return o;
   }
   half4 Frag(V i):SV_Target
   {
    float t=_Time.y;
    float2 uv=i.world.xz/12;
    half3 a=SAMPLE_TEXTURE2D(_Water,sampler_Water,uv+float2(t*0.008,t*0.003)).rgb;
    half3 b=SAMPLE_TEXTURE2D(_Water,sampler_Water,uv*0.73+float2(-t*0.004,t*0.006)).rgb;
    half3 n=normalize(half3(0.10*cos(i.world.x*0.7+t),1,0.08*sin(i.world.z*0.9+t*0.8)));
    half3 view=GetWorldSpaceNormalizeViewDir(i.world);
    half fresnel=pow(1-saturate(dot(n,view)),4);
    Light sun=GetMainLight();
    half spec=pow(saturate(dot(n,normalize(sun.direction+view))),80)*0.45;
    half3 color=lerp((a+b)*0.5,half3(0.30,0.63,0.76),fresnel*0.45);
    color*=SampleSH(n)+sun.color*0.55;
    color+=sun.color*spec;
    return half4(MixFog(color,i.fog),1);
   }
   ENDHLSL
  }
 }
}
