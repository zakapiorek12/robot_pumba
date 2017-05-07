#version 330 core

uniform mat4 projection_matrix;
uniform mat4 cameraview_matrix;
uniform mat4 cameraModel_matrix; //inverse of cameraView_matrix

uniform mat4 object_matrix;

uniform bool drawUnlitScene;

uniform float ambientCoefficient;
uniform vec3 lightPosition;
uniform vec3 lightColor;

uniform vec4 surfaceColor;
uniform float materialSpecExponent;
uniform vec3 specularColor;

uniform int isPlate;
uniform sampler2D tex;

in vec3 fs_position;
in vec3 fs_normal;
in vec2 fs_texturePos;

out vec4 color;

void main(){
	vec3 normal = normalize(transpose(inverse(mat3(object_matrix))) * fs_normal);
	vec3 surfacePos = vec3(object_matrix * vec4(fs_position, 1));
	vec3 surfaceToLight = normalize(lightPosition - surfacePos);
	vec3 surfaceToCamera = normalize((cameraModel_matrix * vec4(0.0, 0.0, 0.0, 1.0)).xyz - surfacePos);

	//ambient
    vec3 ambient = ambientCoefficient * lightColor * surfaceColor.xyz;
	vec3 diffuse = vec3(0.0, 0.0, 0.0);
	vec3 specular = vec3(0.0, 0.0, 0.0);

	if(!drawUnlitScene)
	{
		//diffuse
		float diffuseCoefficient = max(0.0, dot(normal, surfaceToLight));
		diffuse = diffuseCoefficient * specularColor * lightColor;
    
		//specular
		float specularCoefficient = 0.0;
		if(diffuseCoefficient > 0.0)
			specularCoefficient = pow(max(0.0, dot(surfaceToCamera, reflect(-surfaceToLight, normal))), materialSpecExponent);
		specular = specularCoefficient * specularColor * lightColor;
	}
	//color = vec4(normalize(fs_position), 1.0);
	color = vec4(ambient + diffuse + specular, surfaceColor.a);
	if(isPlate == 1)
		color *= texture(tex, fs_texturePos);
}