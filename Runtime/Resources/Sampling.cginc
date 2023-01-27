#ifndef UNITY_POSTFX_SAMPLING
#define UNITY_POSTFX_SAMPLING

// Better, temporally stable box filtering
// [Jimenez14] http://goo.gl/eomGso
// . . . . . . .
// . A . B . C .
// . . D . E . .
// . F . G . H .
// . . I . J . .
// . K . L . M .
// . . . . . . .
float4 DownsampleBox13Tap(sampler2D tex, float2 uv, float2 texelSize)
{
    float4 A = tex2D(tex, uv + texelSize * float2(-1.0, -1.0));
    float4 B = tex2D(tex, uv + texelSize * float2( 0.0, -1.0));
    float4 C = tex2D(tex, uv + texelSize * float2( 1.0, -1.0));
    float4 D = tex2D(tex, uv + texelSize * float2(-0.5, -0.5));
    float4 E = tex2D(tex, uv + texelSize * float2( 0.5, -0.5));
    float4 F = tex2D(tex, uv + texelSize * float2(-1.0,  0.0));
    float4 G = tex2D(tex, uv                                 );
    float4 H = tex2D(tex, uv + texelSize * float2( 1.0,  0.0));
    float4 I = tex2D(tex, uv + texelSize * float2(-0.5,  0.5));
    float4 J = tex2D(tex, uv + texelSize * float2( 0.5,  0.5));
    float4 K = tex2D(tex, uv + texelSize * float2(-1.0,  1.0));
    float4 L = tex2D(tex, uv + texelSize * float2( 0.0,  1.0));
    float4 M = tex2D(tex, uv + texelSize * float2( 1.0,  1.0));

    half2 div = (1.0 / 4.0) * half2(0.5, 0.125);

    float4 o = (D + E + I + J) * div.x;
    o += (A + B + G + F) * div.y;
    o += (B + C + H + G) * div.y;
    o += (F + G + L + K) * div.y;
    o += (G + H + M + L) * div.y;

    return o;
}

// Standard box filtering
float4 DownsampleBox4Tap(sampler2D tex, float2 uv, float2 texelSize)
{
    float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0);

    float4 s;
    s =  tex2D(tex, uv + d.xy);
    s += tex2D(tex, uv + d.zy);
    s += tex2D(tex, uv + d.xw);
    s += tex2D(tex, uv + d.zw);

    return s * (1.0 / 4.0);
}

// 9-tap bilinear upsampler (tent filter)
float4 UpsampleTent(sampler2D tex, float2 uv, float2 texelSize, float4 sampleScale)
{
    float4 d = texelSize.xyxy * float4(1.0, 1.0, -1.0, 0.0) * sampleScale;

    float4 s;
    s =  tex2D(tex, uv - d.xy);
    s += tex2D(tex, uv - d.wy) * 2.0;
    s += tex2D(tex, uv - d.zy);

    s += tex2D(tex, uv + d.zw) * 2.0;
    s += tex2D(tex, uv       ) * 4.0;
    s += tex2D(tex, uv + d.xw) * 2.0;

    s += tex2D(tex, uv + d.zy);
    s += tex2D(tex, uv + d.wy) * 2.0;
    s += tex2D(tex, uv + d.xy);

    return s * (1.0 / 16.0);
}

// Standard box filtering
float4 UpsampleBox(sampler2D tex, float2 uv, float2 texelSize, float4 sampleScale)
{
    float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0) * (sampleScale * 0.5);

    float4 s;
    s =  (tex2D(tex, uv + d.xy));
    s += (tex2D(tex, uv + d.zy));
    s += (tex2D(tex, uv + d.xw));
    s += (tex2D(tex, uv + d.zw));

    return s * (1.0 / 4.0);
}

#endif // UNITY_POSTFX_SAMPLING
