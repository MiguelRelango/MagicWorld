Shader "Hearthbound/Island Blend"
{
 Properties
 {
  _Sand("Sand",2D)="white" {}
  _Grass("Grass",2D)="white" {}
  _Rock("Rock",2D)="white" {}
  _Snow("Snow",2D)="white" {}
  [HideInInspector] _BaseMap("Base",2D)="white" {}
  [HideInInspector] _BaseColor("Color",Color)=(1,1,1,1)
  [HideInInspector] _Cutoff("Cutoff",Float)=0.5
  [HideInInspector] _Cull("Cull",Float)=2
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
  Pass
  {
   Name "Forward"
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma target 3.0
   #pragma multi_compile_fog
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_Sand); SAMPLER(sampler_Sand);
   TEXTURE2D(_Grass); SAMPLER(sampler_Grass);
   TEXTURE2D(_Rock); SAMPLER(sampler_Rock);
   TEXTURE2D(_Snow); SAMPLER(sampler_Snow);
   struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
   struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; half3 normal:TEXCOORD1; half fog:TEXCOORD2; };
   V Vert(A v)
   {
    V o; VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
    o.positionCS=p.positionCS; o.world=p.positionWS;
    o.normal=TransformObjectToWorldNormal(v.normalOS);o.fog=ComputeFogFactor(p.positionCS.z);return o;
   }
   float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
   float Noise(float2 p)
   {
    float2 a=floor(p),b=frac(p); b=b*b*(3-2*b);
    return lerp(lerp(Hash(a),Hash(a+float2(1,0)),b.x),lerp(Hash(a+float2(0,1)),Hash(a+1),b.x),b.y);
   }
   half4 Frag(V i):SV_Target
   {
    half3 n=normalize(i.normal);
    float irregular=(Noise(i.world.xz*0.38)-0.5)*1.25+(Noise(i.world.xz*1.8)-0.5)*0.28;
    float h=i.world.y+irregular;
    half grass=smoothstep(0.65,1.9,h);
    half cliff=1-smoothstep(0.60,0.84,n.y);
    half rock=max(smoothstep(9.8,12.2,h),cliff*smoothstep(0.5,2.5,h));
    half snow=smoothstep(20.0,22.2,h)*(0.25+0.75*smoothstep(0.45,0.8,n.y));
    half3 sand=SAMPLE_TEXTURE2D(_Sand,sampler_Sand,i.world.xz/5).rgb;
    sand*=lerp(0.68,1.0,smoothstep(-0.35,0.65,h));
    half3 g=SAMPLE_TEXTURE2D(_Grass,sampler_Grass,i.world.xz/8).rgb;
    half3 weights=pow(abs(n),4); weights/=max(dot(weights,half3(1,1,1)),0.001);
    half3 r=SAMPLE_TEXTURE2D(_Rock,sampler_Rock,i.world.zy/6).rgb*weights.x
     +SAMPLE_TEXTURE2D(_Rock,sampler_Rock,i.world.xz/6).rgb*weights.y
     +SAMPLE_TEXTURE2D(_Rock,sampler_Rock,i.world.xy/6).rgb*weights.z;
    half3 s=SAMPLE_TEXTURE2D(_Snow,sampler_Snow,i.world.xz/7).rgb;
    half3 base=lerp(lerp(lerp(sand,g,grass),r,rock),s,snow);
    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    float4 shadowCoord=ComputeScreenPos(TransformWorldToHClip(i.world));
    #else
    float4 shadowCoord=TransformWorldToShadowCoord(i.world);
    #endif
    Light sun=GetMainLight(shadowCoord);
    half3 light=SampleSH(n)+sun.color*saturate(dot(n,sun.direction))*sun.shadowAttenuation;
    #if defined(_ADDITIONAL_LIGHTS)
    uint count=GetAdditionalLightsCount();
    for(uint index=0u;index<count;index++)
    {
     Light extra=GetAdditionalLight(index,i.world);
     light+=extra.color*saturate(dot(n,extra.direction))*extra.distanceAttenuation;
    }
    #endif
    return half4(MixFog(base*light,i.fog),1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
