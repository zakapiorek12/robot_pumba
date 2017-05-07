#version 330 core


layout(line_strip, max_vertices = 2) out;

in vec3[] gs_position;
in vec3[] gs_normal;

out vec3 fs_position;
out vec3 fs_normal;
out vec2 fs_texturePos;

void main()
{
	fs_position = gs_position[0];
	fs_normal = gs_normal[0];
	fs_texturePos = vec2(0.0f, 0.5f);

    gl_Position = gs_position[0] + vec4(-0.1, 0.0, 0.0, 0.0);
    EmitVertex();

    gl_Position = gs_position[0] + vec4(0.1, 0.0, 0.0, 0.0);
    EmitVertex();

    EndPrimitive();
}