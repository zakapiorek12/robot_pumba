#version 330 core

precision highp float;

uniform mat4 projection_matrix;
uniform mat4 cameraview_matrix;

layout(location = 0) in vec3 position;
layout(location = 1) in vec3 normal;

//out vec3 gs_position;
out vec3 gs_normal;

void main()
{
    gl_Position = cameraview_matrix * vec4(position, 1.0f);

	//gs_position = gl_Position.xyz;
	gs_normal = normal;
}