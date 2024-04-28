shader_type sky;

global uniform int probe_rendering_pass;

float toplight(vec3 dir)
{
	float v = dot(dir, vec3(0.0,1.0,0.0));
	v = clamp(v, 0.0, 1.0);
	v = smoothstep(0.5f, 0.8f, v);
	return v;
}

void sky() {
	
	if (AT_CUBEMAP_PASS)
	{
		COLOR = vec3(toplight(EYEDIR));
	}
	else
	{
		if (probe_rendering_pass == 2)
		{
			COLOR = vec3(1.0);
		}
		else
		{
			COLOR = vec3(0.0);
		}
	}
}
