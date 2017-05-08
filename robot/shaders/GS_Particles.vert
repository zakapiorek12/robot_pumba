#version 330 core

uniform mat4 projection_matrix;
uniform mat4 cameraview_matrix;
uniform mat4 cameraModel_matrix; //inverse of cameraView_matrix

layout (points) in;
layout (triangle_strip, max_vertices = 6) out;


//in vec3[] gs_position;
in vec3[] gs_normal;

//out vec3 fs_position;
out vec3 fs_normal;
out vec2 fs_texturePos;

void main()
{
	vec4 dy = vec4(0.03f, 0.0f, 0.0f, 0.0f);
	vec4 dx = vec4(0.0f, 0.03f, 0.0f, 0.0f);
	
	fs_normal = gs_normal[0];

	fs_texturePos = vec2(0.0f, 0.0f);
    gl_Position = projection_matrix * (gl_in[0].gl_Position - dx - dy);
    EmitVertex();
	
	fs_texturePos = vec2(0.0f, 1.0f);
    gl_Position = projection_matrix * (gl_in[0].gl_Position - dx + dy);
    EmitVertex();
	
	fs_texturePos = vec2(1.0f, 0.0f);
    gl_Position = projection_matrix * (gl_in[0].gl_Position + dx - dy);
    EmitVertex();
    EndPrimitive();
	
	fs_texturePos = vec2(1.0f, 0.0f);
    gl_Position = projection_matrix * (gl_in[0].gl_Position + dx - dy);
    EmitVertex();
	
	fs_texturePos = vec2(0.0f, 1.0f);
    gl_Position = projection_matrix * (gl_in[0].gl_Position - dx + dy);
    EmitVertex();
	
	fs_texturePos = vec2(1.0f, 1.0f);
    gl_Position = projection_matrix * (gl_in[0].gl_Position + dx + dy);
    EmitVertex();
    EndPrimitive();
}