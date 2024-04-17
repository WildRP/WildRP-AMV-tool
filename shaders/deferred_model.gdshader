shader_type spatial;
render_mode unshaded, world_vertex_coords;

uniform bool blend = false;
uniform sampler2D ab : source_color;
uniform sampler2D nm : hint_normal;

global uniform int probe_rendering_pass;

varying vec3 worldNormal;
void vertex() {
	vec3 n = NORMAL;
	n.z *= -1.0;
	worldNormal = n;
}

void fragment() {
	// Called for every pixel the material is visible on.
	
	vec4 tex = texture(ab, UV);
	ALBEDO = tex.rgb;
	//NORMAL_MAP = texture(nm, UV).rgb;
	
	if (probe_rendering_pass == 1) // normal map pass
	{
		vec4 n = INV_PROJECTION_MATRIX * vec4(NORMAL,1.0);
		//n = INV_VIEW_MATRIX * n;
		ALBEDO = normalize(worldNormal.xzy) * 0.5 + 0.5;
	}
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
