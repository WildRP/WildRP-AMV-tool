shader_type sky;

global uniform int probe_rendering_pass;

void sky() {
	if (AT_CUBEMAP_PASS || probe_rendering_pass == 1)
	{
		COLOR = vec3(1.0);
	}
	else
	{
		COLOR = vec3(0.0);
	}
}
