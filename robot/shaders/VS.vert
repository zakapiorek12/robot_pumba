#version 330 core

precision highp float;

uniform mat4 projection_matrix;
uniform mat4 cameraview_matrix;
uniform mat4 object_matrix;

layout(location = 0) in vec3 position;
layout(location = 1) in vec3 normals;

void main()
{
    gl_Position = projection_matrix * cameraview_matrix * object_matrix * vec4(position, 1.0f);
}