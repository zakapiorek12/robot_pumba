#version 330 core

in vec2 fs_texturePos;
in vec3 fs_normal;

uniform sampler2D tex;

out vec4 color;

void main()
{
	float alpha = 1.0f - fs_normal.x;
	if(alpha <= 0.01)
		color = vec4(0, 0, 0, 0);
	else
		color = vec4(texture(tex, fs_texturePos).xyz, 1.0f - fs_normal.x);  
} 