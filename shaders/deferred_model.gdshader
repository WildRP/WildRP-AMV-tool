shader_type spatial;
render_mode unshaded;

uniform bool blend = false;
uniform sampler2D ab : source_color;
uniform sampler2D nm : hint_normal;

void fragment() {
	// Called for every pixel the material is visible on.
	
	vec4 tex = texture(ab, UV);
	ALBEDO = tex.rgb;
	NORMAL_MAP = texture(nm, UV).rgb;
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
