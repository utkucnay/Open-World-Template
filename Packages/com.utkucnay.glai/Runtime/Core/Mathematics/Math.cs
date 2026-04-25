using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Glai
{
    public static class math
    {
        public const float PI = Unity.Mathematics.math.PI;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int abs(int x) => Unity.Mathematics.math.abs(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float abs(float x) => Unity.Mathematics.math.abs(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double abs(double x) => Unity.Mathematics.math.abs(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 abs(float2 x) => Unity.Mathematics.math.abs(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 abs(float3 x) => Unity.Mathematics.math.abs(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 abs(float4 x) => Unity.Mathematics.math.abs(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int min(int x, int y) => Unity.Mathematics.math.min(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint min(uint x, uint y) => Unity.Mathematics.math.min(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float min(float x, float y) => Unity.Mathematics.math.min(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double min(double x, double y) => Unity.Mathematics.math.min(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 min(float2 x, float2 y) => Unity.Mathematics.math.min(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 min(float3 x, float3 y) => Unity.Mathematics.math.min(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 min(float4 x, float4 y) => Unity.Mathematics.math.min(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int max(int x, int y) => Unity.Mathematics.math.max(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint max(uint x, uint y) => Unity.Mathematics.math.max(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float max(float x, float y) => Unity.Mathematics.math.max(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double max(double x, double y) => Unity.Mathematics.math.max(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 max(float2 x, float2 y) => Unity.Mathematics.math.max(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 max(float3 x, float3 y) => Unity.Mathematics.math.max(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 max(float4 x, float4 y) => Unity.Mathematics.math.max(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int clamp(int x, int a, int b) => Unity.Mathematics.math.clamp(x, a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint clamp(uint x, uint a, uint b) => Unity.Mathematics.math.clamp(x, a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float clamp(float x, float a, float b) => Unity.Mathematics.math.clamp(x, a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double clamp(double x, double a, double b) => Unity.Mathematics.math.clamp(x, a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 clamp(float2 x, float2 a, float2 b) => Unity.Mathematics.math.clamp(x, a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 clamp(float3 x, float3 a, float3 b) => Unity.Mathematics.math.clamp(x, a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 clamp(float4 x, float4 a, float4 b) => Unity.Mathematics.math.clamp(x, a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float saturate(float x) => Unity.Mathematics.math.saturate(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 saturate(float2 x) => Unity.Mathematics.math.saturate(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 saturate(float3 x) => Unity.Mathematics.math.saturate(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 saturate(float4 x) => Unity.Mathematics.math.saturate(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float lerp(float x, float y, float s) => Unity.Mathematics.math.lerp(x, y, s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 lerp(float2 x, float2 y, float s) => Unity.Mathematics.math.lerp(x, y, s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 lerp(float3 x, float3 y, float s) => Unity.Mathematics.math.lerp(x, y, s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 lerp(float4 x, float4 y, float s) => Unity.Mathematics.math.lerp(x, y, s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float sin(float x) => Unity.Mathematics.math.sin(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 sin(float2 x) => Unity.Mathematics.math.sin(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 sin(float3 x) => Unity.Mathematics.math.sin(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 sin(float4 x) => Unity.Mathematics.math.sin(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float cos(float x) => Unity.Mathematics.math.cos(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 cos(float2 x) => Unity.Mathematics.math.cos(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 cos(float3 x) => Unity.Mathematics.math.cos(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 cos(float4 x) => Unity.Mathematics.math.cos(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float radians(float x) => Unity.Mathematics.math.radians(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 radians(float2 x) => Unity.Mathematics.math.radians(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 radians(float3 x) => Unity.Mathematics.math.radians(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 radians(float4 x) => Unity.Mathematics.math.radians(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float degrees(float x) => Unity.Mathematics.math.degrees(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 degrees(float2 x) => Unity.Mathematics.math.degrees(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 degrees(float3 x) => Unity.Mathematics.math.degrees(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 degrees(float4 x) => Unity.Mathematics.math.degrees(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float sqrt(float x) => Unity.Mathematics.math.sqrt(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 sqrt(float2 x) => Unity.Mathematics.math.sqrt(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 sqrt(float3 x) => Unity.Mathematics.math.sqrt(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 sqrt(float4 x) => Unity.Mathematics.math.sqrt(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float round(float x) => Unity.Mathematics.math.round(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 round(float2 x) => Unity.Mathematics.math.round(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 round(float3 x) => Unity.Mathematics.math.round(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 round(float4 x) => Unity.Mathematics.math.round(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float floor(float x) => Unity.Mathematics.math.floor(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ceil(float x) => Unity.Mathematics.math.ceil(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float length(float2 x) => Unity.Mathematics.math.length(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float length(float3 x) => Unity.Mathematics.math.length(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float length(float4 x) => Unity.Mathematics.math.length(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float lengthsq(float2 x) => Unity.Mathematics.math.lengthsq(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float lengthsq(float3 x) => Unity.Mathematics.math.lengthsq(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float lengthsq(float4 x) => Unity.Mathematics.math.lengthsq(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float distance(float2 x, float2 y) => Unity.Mathematics.math.distance(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float distance(float3 x, float3 y) => Unity.Mathematics.math.distance(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float distance(float4 x, float4 y) => Unity.Mathematics.math.distance(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float dot(float2 x, float2 y) => Unity.Mathematics.math.dot(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float dot(float3 x, float3 y) => Unity.Mathematics.math.dot(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float dot(float4 x, float4 y) => Unity.Mathematics.math.dot(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 cross(float3 x, float3 y) => Unity.Mathematics.math.cross(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 normalize(float2 x) => Unity.Mathematics.math.normalize(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 normalize(float3 x) => Unity.Mathematics.math.normalize(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 normalize(float4 x) => Unity.Mathematics.math.normalize(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion normalize(quaternion q) => Unity.Mathematics.math.normalize(q);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 normalizesafe(float2 x) => Unity.Mathematics.math.normalizesafe(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 normalizesafe(float3 x) => Unity.Mathematics.math.normalizesafe(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 normalizesafe(float4 x) => Unity.Mathematics.math.normalizesafe(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 normalizesafe(float2 x, float2 defaultvalue) => Unity.Mathematics.math.normalizesafe(x, defaultvalue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 normalizesafe(float3 x, float3 defaultvalue) => Unity.Mathematics.math.normalizesafe(x, defaultvalue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 normalizesafe(float4 x, float4 defaultvalue) => Unity.Mathematics.math.normalizesafe(x, defaultvalue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion normalizesafe(quaternion q) => Unity.Mathematics.math.normalizesafe(q);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion normalizesafe(quaternion q, quaternion defaultvalue) => Unity.Mathematics.math.normalizesafe(q, defaultvalue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 mul(float2x2 a, float2 b) => Unity.Mathematics.math.mul(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 mul(float3x3 a, float3 b) => Unity.Mathematics.math.mul(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 mul(float4x4 a, float4 b) => Unity.Mathematics.math.mul(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 mul(quaternion q, float3 v) => Unity.Mathematics.math.mul(q, v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion mul(quaternion a, quaternion b) => Unity.Mathematics.math.mul(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 mul(float4x4 a, float4x4 b) => Unity.Mathematics.math.mul(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion quaternion(float3x3 m) => Unity.Mathematics.math.quaternion(m);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion quaternion(float4x4 m) => Unity.Mathematics.math.quaternion(m);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int select(int a, int b, bool c) => Unity.Mathematics.math.select(a, b, c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint select(uint a, uint b, bool c) => Unity.Mathematics.math.select(a, b, c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float select(float a, float b, bool c) => Unity.Mathematics.math.select(a, b, c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 select(float2 a, float2 b, bool2 c) => Unity.Mathematics.math.select(a, b, c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 select(float3 a, float3 b, bool3 c) => Unity.Mathematics.math.select(a, b, c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 select(float4 a, float4 b, bool4 c) => Unity.Mathematics.math.select(a, b, c);
    }

    public static class ByteSizeHelper
    {
        public const int BytesPerKilobyte = 1024;
        public const int BytesPerMegabyte = BytesPerKilobyte * 1024;
        public const long BytesPerGigabyte = (long)BytesPerMegabyte * 1024L;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int B(int value) => value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long B(long value) => value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int KB(int value) => value * BytesPerKilobyte;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long KB(long value) => value * BytesPerKilobyte;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MB(int value) => value * BytesPerMegabyte;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long MB(long value) => value * BytesPerMegabyte;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GB(int value) => value * BytesPerGigabyte;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GB(long value) => value * BytesPerGigabyte;
    }
}

namespace Glai.Mathematics
{
    public static class Math
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GB(int value)
        {
            return Glai.ByteSizeHelper.GB(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MB(int value)
        {
            return Glai.ByteSizeHelper.MB(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int KB(int value)
        {
            return Glai.ByteSizeHelper.KB(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int B(int value)
        {
            return Glai.ByteSizeHelper.B(value);
        }
    }
}
