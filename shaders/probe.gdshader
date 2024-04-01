shader_type spatial;

render_mode unshaded;

instance uniform vec3 positive_occlusion;
instance uniform vec3 negative_occlusion;

float ToSRGB(float RGB)
{
  float S1 = sqrt(RGB);
  float S2 = sqrt(S1);
  float S3 = sqrt(S2);
  return 0.662002687 * S1 + 0.684122060 * S2 - 0.323583601 * S3 - 0.0225411470 * RGB;
}

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	
	vec3 worldNormal = (INV_VIEW_MATRIX * vec4(NORMAL, 0.0)).xyz;
	
	vec3 pos_blend = clamp(sign(worldNormal), 0, 1);
	vec3 neg_blend = clamp(sign(-worldNormal), 0, 1);
	float x = positive_occlusion.x * pos_blend.x + negative_occlusion.x * neg_blend.x;
	float y = positive_occlusion.y * pos_blend.y + negative_occlusion.y * neg_blend.y;
	float z = positive_occlusion.z * pos_blend.z + negative_occlusion.z * neg_blend.z;
	
	
	vec3 blend = pow(abs(worldNormal), vec3(2.0));
	blend /= dot(blend, vec3(1.0));
	float result = x * blend.x + y * blend.y + z * blend.z;
	
	if (OUTPUT_IS_SRGB) result = ToSRGB(result);
	
	ALBEDO = vec3(result);
	
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
