#version 330 core

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

out vec4 color;

void main(){
	vec3 normal = normalize(transpose(inverse(mat3(object_matrix))) * fs_normal);
	vec3 surfacePos = vec3(object_matrix * vec4(fs_position, 1));
	vec3 surfaceToLight = normalize(lightPosition - surfacePos);
	vec3 surfaceToCamera = normalize((cameraModel_matrix * vec4(0.0, 0.0, 0.0, 1.0)).xyz - surfacePos);

	//ambient
    vec3 ambient = ambientCoefficient * lightColor * surfaceColor.xyz;

    //diffuse
    float diffuseCoefficient = max(0.0, dot(normal, surfaceToLight));
    vec3 diffuse = diffuseCoefficient * specularColor * lightColor;
    
    //specular
    float specularCoefficient = 0.0;
    if(diffuseCoefficient > 0.0)
        specularCoefficient = pow(max(0.0, dot(surfaceToCamera, reflect(-surfaceToLight, normal))), materialSpecExponent);
    vec3 specular = specularCoefficient * specularColor * lightColor;

	//color = vec4(normalize(fs_position), 1.0);
    color = vec4(ambient + diffuse + specular, surfaceColor.a);
}