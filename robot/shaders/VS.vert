#version 330 core

precision highp float;

uniform mat4 projection_matrix;
uniform mat4 cameraview_matrix;
uniform mat4 object_matrix;

//TODO normalne - vec3 zmienić
layout(location = 0) in vec3 position;

void main()
{
    gl_Position = projection_matrix * cameraview_matrix * object_matrix * vec4(position, 1.0f);
}