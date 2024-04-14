shader_type spatial;
render_mode unshaded, cull_disabled;

uniform sampler2D screen_texture : source_color, hint_screen_texture;
uniform sampler2D depth_texture : source_color, hint_depth_texture;
uniform sampler2D normal_texture : source_color, hint_normal_roughness_texture;

global uniform int probe_rendering_pass;

void vertex() {
	POSITION = vec4(VERTEX, 1.0);
}

void fragment() {
	if (probe_rendering_pass == 0) // Albedo
		ALBEDO = texture(screen_texture, SCREEN_UV).rgb;
	if (probe_rendering_pass == 1) // Normal
		ALBEDO = texture(normal_texture, SCREEN_UV).rgb;
	if (probe_rendering_pass == 2) // Occlusion
		ALBEDO = texture(screen_texture, SCREEN_UV).rgb;
	if (probe_rendering_pass == 3) // Sky mask
	{
		float depth = texture(depth_texture, SCREEN_UV).r;
		
		ALBEDO = vec3(depth > 0.99 ? 0.0 : 1.0);
	}	
	if (probe_rendering_pass == 4) // Depth
	{
		float depth = texture(depth_texture, SCREEN_UV).r;
		vec3 ndc = vec3(SCREEN_UV * 2.0 - 1.0, depth);
		vec4 view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	  	view.xyz /= view.w;
	  	float linear_depth = -view.z;
		ALBEDO = vec3(linear_depth / 100.0);
	}
		
}

//void light() {
	// Called for every pixel for every light affecting the CanvasItem.
	// Uncomment to replace the default light processing function with this one.
//}
