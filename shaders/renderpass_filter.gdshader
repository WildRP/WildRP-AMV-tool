shader_type spatial;
render_mode unshaded, cull_disabled;

uniform sampler2D screen_texture : hint_screen_texture;
uniform sampler2D depth_texture : hint_depth_texture;
uniform sampler2D normal_texture : hint_normal_roughness_texture;

global uniform int probe_rendering_pass;


// Converts a color from linear light gamma to sRGB gamma
vec3 fromLinear(vec3 linearRGB)
{
    bvec3 cutoff = lessThan(linearRGB.rgb, vec3(0.0031308));
    vec3 higher = vec3(1.055)*pow(linearRGB.rgb, vec3(1.0/2.4)) - vec3(0.055);
    vec3 lower = linearRGB.rgb * vec3(12.92);

    return mix(higher, lower, cutoff);
}

varying mat4 CAMERA;

void vertex() {
	POSITION = vec4(VERTEX, 1.0);
	
	CAMERA = INV_VIEW_MATRIX;
}

void fragment() {
	
	if (probe_rendering_pass == 1)
	{
		mat3 view_to_world_normal_mat = mat3(
            INV_VIEW_MATRIX[0].xyz, 
            INV_VIEW_MATRIX[1].xyz,
            INV_VIEW_MATRIX[2].xyz);
		
		vec3 n = texture(normal_texture, SCREEN_UV).xyz;
		n = n * 2.0 - 1.0;
		n = view_to_world_normal_mat * n;
		n = normalize(n.xzy);
		ALBEDO = fromLinear(n * 0.5 + 0.5);
	}
	else if (probe_rendering_pass == 4) // Depth
	{
		float depth = texture(depth_texture, SCREEN_UV).r;
		vec3 ndc = vec3(SCREEN_UV * 2.0 - 1.0, depth);
		vec4 view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	  	view.xyz /= view.w;
	  	float linear_depth = -view.z;
		ALBEDO = vec3(linear_depth / 150.0);
		
		/*if (linear_depth > 149.0)
			ALBEDO = vec3(1.0);
		else
		{
			ALBEDO = vec3(.1);
		}*/
	}
	else
	{
		ALBEDO = texture(screen_texture, SCREEN_UV).rgb;
	}
		
}

//void light() {
	// Called for every pixel for every light affecting the CanvasItem.
	// Uncomment to replace the default light processing function with this one.
//}
