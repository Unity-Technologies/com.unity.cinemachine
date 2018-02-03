// Subset of PostProcessing's Colors.hlsl

#ifndef UNITY_CMWAVEFORM_COLORS
#define UNITY_CMWAVEFORM_COLORS

#include "StdLib.hlsl"

half3 LinearToSRGB(half3 c)
{
	half3 sRGBLo = c * 12.92;
	half3 sRGBHi = (PositivePow(c, half3(1.0 / 2.4, 1.0 / 2.4, 1.0 / 2.4)) * 1.055) - 0.055;
	half3 sRGB = (c <= 0.0031308) ? sRGBLo : sRGBHi;
	return sRGB;
}

#endif // UNITY_CMWAVEFORM_COLORS
