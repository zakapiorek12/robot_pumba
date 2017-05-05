﻿#version 330 core

uniform mat4 projection_matrix;
uniform mat4 cameraview_matrix;
uniform mat4 cameraModel_matrix; //inverse of cameraView_matrix

uniform mat4 object_matrix;

uniform float ambientCoefficient;
uniform vec3 lightPosition;
uniform vec3 lightColor;

uniform vec4 surfaceColor;
uniform float materialSpecExponent;
uniform vec3 specularColor;

in vec3 fs_position;
in vec3 fs_normal;
in vec3 vs_position;

out vec4 color;

void main(){
	if(vs_position.x <= -1)
	{
		color = vec4(0.0f, 0.0f, 0.0f, 0.0f);
		return;
	}
	vec3 surfaceToLight = normalize(lightPosition - fs_position);
	vec3 surfaceToCamera = normalize((cameraModel_matrix * vec4(0.0, 0.0, 0.0, 1.0)).xyz - fs_position);

	//ambient
    vec3 ambient = ambientCoefficient * lightColor * surfaceColor.xyz;

    //diffuse
    float diffuseCoefficient = max(0.0, dot(fs_normal, surfaceToLight));
    vec3 diffuse = diffuseCoefficient * surfaceColor.xyz * lightColor;
    
    //specular
    float specularCoefficient = 0.0;
    if(diffuseCoefficient > 0.0)
        specularCoefficient = pow(max(0.0, dot(surfaceToCamera, reflect(-surfaceToLight, fs_normal))), materialSpecExponent);
    vec3 specular = specularCoefficient * specularColor * lightColor;

	//color = vec4(normalize(fs_position), 1.0);
    color = vec4(ambient + diffuse + specular, surfaceColor.w);
}