// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

#ifndef UNATURE_Foliage_BASE
#define UNATURE_Foliage_BASE

uniform float _Cutoff;
uniform float4 _FoliageInteractionPosition;

// wind mechanics

uniform float _WindSpeed;
uniform float _WindBending;

void FastSinCos(float4 val, out float4 s, out float4 c) {
	val = val * 6.408849 - 3.1415927;
	float4 r5 = val * val;
	float4 r6 = r5 * r5;
	float4 r7 = r6 * r5;
	float4 r8 = r6 * r5;
	float4 r1 = r5 * val;
	float4 r2 = r1 * r5;
	float4 r3 = r2 * r5;
	float4 sin7 = { 1, -0.16161616, 0.0083333, -0.00019841 };
	float4 cos8 = { -0.5, 0.041666666, -0.0013888889, 0.000024801587 };
	s = val + r1 * sin7.y + r2 * sin7.z + r3 * sin7.w;
	c = 1 + r5 * cos8.x + r6 * cos8.y + r7 * cos8.z + r8 * cos8.w;
}

float4 ApplyFastWind(float4 vertex, float texCoordY, float windSpeedAdjuster)
{
	if (_WindSpeed == 0) return vertex;

	if (windSpeedAdjuster == 0) windSpeedAdjuster = 1;

	float speed = _WindSpeed * windSpeedAdjuster;

	float4 _waveXmove = float4 (0.024, 0.04, -0.12, 0.096);
	float4 _waveZmove = float4 (0.006, .02, -0.02, 0.1);

	const float4 waveSpeed = float4 (1.2, 2, 1.6, 4.8);

	float4 waves;
	waves = vertex.x * _WindBending;
	waves += vertex.z * _WindBending;

	waves += _Time.x * (1 - 0.4) * waveSpeed * speed;

	float4 s, c;
	waves = frac(waves);
	FastSinCos(waves, s, c);

	float waveAmount = texCoordY * (1 + 0.4);
	s *= waveAmount;

	s *= normalize(waveSpeed);

	s = s * s;
	float fade = dot(s, 1.3);
	s = s * s;

	float3 waveMove = float3 (0, 0, 0);
	waveMove.x = dot(s, _waveXmove);
	waveMove.z = dot(s, _waveZmove);

	vertex.xz -= waveMove.xz;

	//vertex -= mul(_World2Object, float3(_WindSpeed, 0, _WindSpeed)).x * _WindBending * _SinTime;

	return vertex;
}

// color maps 
uniform sampler2D _ColorMap;
uniform sampler2D _GrassMap;
uniform sampler2D _WorldMap;

// global settings

uniform int _FoliageAreaSize;
uniform int _FoliageAreaResolution;
uniform float4 _FoliageAreaPosition;

uniform float _DensityMultiplier;
uniform float _NoiseMultiplier;

uniform float _MinimumWidth;
uniform float _MaximumWidth;

uniform float _MinimumHeight;
uniform float _MaximumHeight;

uniform float _UseColorMap;

uniform half4 _dryColor;
uniform half4 _healthyColor;

uniform float fadeDistance = 100;

// lods

uniform float lods_Enabled;

uniform float lod0_Distance;
uniform float lod0_Value;

uniform float lod1_Distance;
uniform float lod1_Value;

uniform float lod2_Distance;
uniform float lod2_Value;

uniform float lod3_Distance;
uniform float lod3_Value;

// interactions

uniform sampler2D _InteractionMap;
uniform int _InteractionMapRadius;
uniform int _InteractionResolution;
float4 _InteractionTouchBendedInstances[20];

uniform float touchBendingEnabled;
uniform float touchBendingStrength;

// Property Block
float4 _WorldPosition;
float4 _StreamingAdjuster;

struct uNature_Foliage_appdata
{
	float4 vertex : POSITION;
	float3 normal : NORMAL;

	float4 texcoord : TEXCOORD0;
	float4 texcoord1 : TEXCOORD1;
	float4 texcoord2 : TEXCOORD2;
	float4 texcoord3 : TEXCOORD3;
	float4 color : COLOR;
};

float4 GetInformationFromMap(float3 worldPos, float resolution, sampler2D map)
{
	worldPos.x = clamp(worldPos.x, 0, resolution - 1);
	worldPos.z = clamp(worldPos.z, 0, resolution - 1);

	return tex2Dlod(map, float4(worldPos.x / resolution, worldPos.z / resolution, 0, 0));
}

float InterpolatedSingle(float cord, float difference, float currentHeight)
{
	float remainer = cord - floor(cord);

	return currentHeight + (difference * remainer);
}

float InterpolateValue(float xVertex, float zVertex, float currentHeight, float nextHeight)
{
	float difference = nextHeight - currentHeight;

	return InterpolatedSingle(zVertex, difference, currentHeight);
}

half3 GetDryHealthy(float spread)
{
	float3 differences = _healthyColor.rgb - _dryColor.rgb;

	return _dryColor.xyz + differences.xyz * spread;
	//return float4(_dryColor.x + diferences.x * spread, _dryColor.y + diferences.y * spread, _dryColor.z + diferences.z * spread, 1);
}

float GetSize(float noise, float max, float min)
{
	float difference = max - min;
	difference *= noise;

	return min + difference;
}

float InverseNegativeConvertedToPositive(float value)
{
	value -= 0.5;
	value *= 2;

	return value;
}

float CalculateNormalBChannel(float x, float y)
{
	return sqrt(1 - (x*x + y*y));
}

float4 CalculateHeightsAndNormals(float3 vertexNoisedPosition)
{
	float4 results;

	float _FoliageWorldDifferences = (float)_FoliageAreaSize / _FoliageAreaResolution;

	//calculate interpolated heights
	float3 flooredVertexWorldPosition = floor(vertexNoisedPosition);
	float3 ceiledVertexWorldPosition = ceil(vertexNoisedPosition);

	float4 flooredWorldMap = GetInformationFromMap(flooredVertexWorldPosition / _FoliageWorldDifferences, _FoliageAreaResolution, _WorldMap);
	float4 ceiledWorldMap = GetInformationFromMap(ceiledVertexWorldPosition / _FoliageWorldDifferences, _FoliageAreaResolution, _WorldMap);

	float2 flooredHeightNormalized = flooredWorldMap.ba * 255;
	float2 ceiledHeightNormalized = ceiledWorldMap.ba * 255;

	float flooredHeightTransformed = (((flooredHeightNormalized.x * 256.0) + flooredHeightNormalized.y) / 65535.0) * _FoliageAreaResolution;
	float ceiledHeightTransformed = (((ceiledHeightNormalized.x * 256.0) + ceiledHeightNormalized.y) / 65535.0) * _FoliageAreaResolution;

	//calculate results (heights + normals)

	results.x = 1;
	results.y = InterpolateValue(vertexNoisedPosition.x, vertexNoisedPosition.z, InverseNegativeConvertedToPositive(flooredWorldMap.x), InverseNegativeConvertedToPositive(ceiledWorldMap.x));
	results.z = InterpolateValue(vertexNoisedPosition.x, vertexNoisedPosition.z, InverseNegativeConvertedToPositive(flooredWorldMap.y), InverseNegativeConvertedToPositive(ceiledWorldMap.y));
	results.w = InterpolateValue(vertexNoisedPosition.x, vertexNoisedPosition.z, flooredHeightTransformed, ceiledHeightTransformed);

	return results;
}

float GetLOD(float distanceFromCamera)
{
	if (lods_Enabled == 0 || distanceFromCamera < lod0_Distance) return 1;

	if (distanceFromCamera >= lod0_Distance && distanceFromCamera < lod1_Distance) return lod0_Value;
	else if (distanceFromCamera >= lod1_Distance && distanceFromCamera < lod2_Distance) return lod1_Value;
	else if (distanceFromCamera >= lod2_Distance && distanceFromCamera < lod3_Distance) return lod2_Value;
	else return lod3_Value;
}

float4 CalculateTouchBending(float4 vertex)
{
	if (touchBendingEnabled != 1) return vertex;

	float4 current;
	for (int i = 0; i < 20; i++)
	{
		current = _InteractionTouchBendedInstances[i];

		if (current.w > 0)
		{
			if (distance(vertex.xz, current.xz) <= current.w)
			{
				float WMDistance = 1 - clamp(distance(vertex.xyz, current.xyz) / current.w, 0, 1);
				float3 posDifferences = normalize(vertex.xyz - current.xyz);

				float3 strengthedDifferences  = posDifferences * touchBendingStrength;

				float3 resultXZ = WMDistance * strengthedDifferences;

				vertex.xz += resultXZ.xz;
				vertex.y -= WMDistance * touchBendingStrength;

				return vertex;
			}
		}
	}

	return vertex;
}

void CalculateGPUVertex(inout uNature_Foliage_appdata v)
{
	float _FoliageAreaMultiplier = (float)_FoliageAreaResolution / _FoliageAreaSize;

	v.vertex.xz /= _FoliageAreaMultiplier;

	float4 StreamedWorldPosition = _WorldPosition + _StreamingAdjuster;

	float distanceFromCamera = distance(v.vertex.xz + _WorldPosition.xz, _WorldSpaceCameraPos.xz - _FoliageAreaPosition.xz);

	if (distanceFromCamera > fadeDistance) return;

	float4 vertexInformation = GetInformationFromMap((v.vertex.xyz + StreamedWorldPosition.xyz) * _FoliageAreaMultiplier, _FoliageAreaResolution, _GrassMap);
	float density = vertexInformation.z * 255 * _DensityMultiplier * GetLOD(distanceFromCamera);

	if (density >= v.texcoord3.y)
	{
		float persistentNoise = vertexInformation.a;

		float3 noise = float3(v.texcoord2.x, 0, v.texcoord2.y) * _NoiseMultiplier;

		float xNoisePosition = v.vertex.x + noise.x;
		float zNoisePosition = v.vertex.z + noise.z;

		float xNoiseWorldPosition = xNoisePosition + StreamedWorldPosition.x;
		float zNoiseWorldPosition = zNoisePosition + StreamedWorldPosition.z;

		float3 position = float3(xNoiseWorldPosition, 0, zNoiseWorldPosition);

		if (v.texcoord1.y > 0.15)
		{
			v.texcoord1.y *= GetSize(v.texcoord2.x, _MinimumHeight, _MaximumHeight);
		}

		float widthValue = GetSize(v.texcoord2.x, _MinimumWidth, _MaximumWidth);
		v.texcoord1.x *= widthValue;
		v.texcoord3.x *= widthValue;

		float3 pos = float3(v.texcoord1.x, v.texcoord1.y, v.texcoord3.x);

		float interactionDifferences = (float)_InteractionMapRadius / _InteractionResolution;
		float4 interactionInformation = GetInformationFromMap(_FoliageInteractionPosition.xyz / interactionDifferences, _InteractionResolution, _InteractionMap);

		float4 worldInformation = CalculateHeightsAndNormals(position);

		v.vertex.y += worldInformation.w;

		float yEuler = v.texcoord2.x * 360;

		float4 q = float4(0, cos(0) * sin(yEuler) * cos(0) + sin(0) * cos(yEuler) * sin(0), 0, cos(0) * cos(yEuler) * cos(0) - sin(0) * sin(yEuler) * sin(0));

		float3 t = 2 * cross(q.xyz, pos);
		pos += q.w * t + cross(q.xyz, t);

		t = 2 * cross(q.xyz, v.normal);
		v.normal = v.normal + q.w * t + cross(q.xyz, t);

		v.vertex.xyz += pos + noise + _WorldPosition.xyz + _FoliageAreaPosition.xyz;

		v.vertex = CalculateTouchBending(v.vertex);

		v.vertex = ApplyFastWind(v.vertex, v.texcoord.y, (interactionInformation.r * 255));

		if (_UseColorMap == 1) // if use color map == 1 (like a boolean == true)
		{
			v.color = GetInformationFromMap(position, _FoliageAreaSize, _ColorMap);
		}
		else
		{
			v.color.xyz = GetDryHealthy(persistentNoise);
		}
	}
}

#endif