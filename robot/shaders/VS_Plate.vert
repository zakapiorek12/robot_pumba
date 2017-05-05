#version 330 core

precision highp float;

uniform mat4 projection_matrix;
uniform mat4 cameraview_matrix;
uniform mat4 object_matrix;

layout(location = 0) in vec3 position;
layout(location = 1) in vec3 normal;

out vec3 fs_position;
out vec3 fs_normal;
out vec3 vs_position;

void main()
{
    gl_Position = projection_matrix * cameraview_matrix * object_matrix * vec4(position, 1.0f);

	vs_position = gl_Position.xyz;
	fs_position = position;
	fs_normal = normal;
}