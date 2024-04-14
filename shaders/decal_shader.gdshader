shader_type spatial;
render_mode unshaded, depth_prepass_alpha;

uniform bool blend = false;
uniform sampler2D ab : source_color;
uniform sampler2D nm : hint_normal;

varying float vertAlpha;
void vertex() {
	vertAlpha = COLOR.a;
}

void fragment() {
	// Called for every pixel the material is visible on.
	
	vec4 tex = texture(ab, UV);
	ALBEDO = tex.rgb;
	NORMAL_MAP = texture(nm, UV).rgb;
	
	ALPHA = tex.a * vertAlpha;
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
