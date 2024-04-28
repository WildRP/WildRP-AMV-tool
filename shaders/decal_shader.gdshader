shader_type spatial;
render_mode unshaded, depth_prepass_alpha, cull_disabled;

uniform bool blend = false;
uniform sampler2D ab : source_color;
uniform sampler2D nm : hint_normal;

global uniform int probe_rendering_pass;

uniform bool isWindow = false;

varying float vertAlpha;
varying vec3 worldNormal;
void vertex() {
	vertAlpha = COLOR.a;
	worldNormal = NORMAL;
}

void fragment() {
	// Called for every pixel the material is visible on.
	
	vec4 tex = texture(ab, UV);
	ALBEDO = tex.rgb;
	//NORMAL_MAP = texture(nm, UV).rgb;
	
	ALPHA = vertAlpha * tex.a;
	
	if (probe_rendering_pass == 1) // normal map pass
	{
		ALPHA = 0.0;
	}
	if (probe_rendering_pass == 3 && isWindow)
	{
		ALPHA = 1.0;
		ALBEDO = vec3(0.0);
	}
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
