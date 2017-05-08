using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK.Graphics.OpenGL;

namespace robot
{
    class ShaderProgram
    {
        public int ProgramID = -1;
        public int VShaderID = -1;
        public int FShaderID = -1;
        public int GShaderID = -1;
        public int AttributeCount = 0;
        public int UniformCount = 0;

        public Dictionary<String, AttributeInfo> Attributes = new Dictionary<string, AttributeInfo>();
        public Dictionary<String, UniformInfo> Uniforms = new Dictionary<string, UniformInfo>();
        public Dictionary<String, uint> Buffers = new Dictionary<string, uint>();
        //index bufor widnieje pod kluczem "index" w slowniku powyzej, 
        //pozostale klucze maja taka sama nazwe jak atrybut w shaderze, ktorego dotycza

        public ShaderProgram()
        {
            ProgramID = GL.CreateProgram();
        }

        private void loadShader(String code, ShaderType type, out int address)
        {
            address = GL.CreateShader(type);
            GL.ShaderSource(address, code);
            GL.CompileShader(address);
            GL.AttachShader(ProgramID, address);
            string errors = GL.GetShaderInfoLog(address);
            if (!string.IsNullOrEmpty(errors))
                MessageBox.Show(errors);
        }

        public void LoadShaderFromString(String code, ShaderType type)
        {
            if (type == ShaderType.VertexShader)
            {
                loadShader(code, type, out VShaderID);
            }
            else if (type == ShaderType.FragmentShader)
            {
                loadShader(code, type, out FShaderID);
            }
            else if (type == ShaderType.GeometryShader)
                loadShader(code, type, out GShaderID);
        }

        public string LoadShaderFromFile(String filename, ShaderType type)
        {
            string code = "";
            using (StreamReader sr = new StreamReader(filename))
            {
                if (type == ShaderType.VertexShader)
                {
                    code = sr.ReadToEnd();
                    loadShader(code, type, out VShaderID);
                }
                else if (type == ShaderType.FragmentShader)
                {
                    code = sr.ReadToEnd();
                    loadShader(code, type, out FShaderID);
                }
                else if (type == ShaderType.GeometryShader)
                {
                    code = sr.ReadToEnd();
                    loadShader(code, type, out GShaderID);
                }
            }
            return code;
        }

        public void Link(params string[] shadersCode)
        {
            GL.LinkProgram(ProgramID);

            string errors = GL.GetProgramInfoLog(ProgramID);
            if (!string.IsNullOrEmpty(errors))
                MessageBox.Show(errors);

            GL.GetProgram(ProgramID, GetProgramParameterName.ActiveAttributes, out AttributeCount);
            GL.GetProgram(ProgramID, GetProgramParameterName.ActiveUniforms, out UniformCount);

            string vsfsCode = "";
            foreach (string s in shadersCode)
                vsfsCode += s;
            vsfsCode = Regex.Replace(vsfsCode, @"\s+", " "); // kasowanie podwojnych bialych znakow - aby byl co najwyzej jeden miedzy wyrazami
            vsfsCode = vsfsCode.Replace("\r\n", "\n");
            string[] lines = vsfsCode.Split(';');
            List<string> attributesNames = new List<string>();
            List<string> outNames = new List<string>();
            foreach (string s in lines)
            {
                string varName = null;
                if (s.Contains(" uniform ") || s.Contains(" in ") || s.Contains(" out "))
                    varName = s.Substring(s.LastIndexOf(' ') + 1);
                if (s.Contains(" uniform "))
                {
                    UniformInfo uniform = new UniformInfo();
                    uniform.address = GL.GetUniformLocation(ProgramID, varName);
                    uniform.name = varName;
                    if (!Uniforms.ContainsKey(varName))
                        Uniforms.Add(varName, uniform);
                }
                else if (s.Contains(" in "))
                    attributesNames.Add(varName);
                else if (s.Contains(" out "))
                    outNames.Add(varName);
            }
            attributesNames.RemoveAll(s => outNames.Contains(s));
            foreach (string s in attributesNames)
            {
                AttributeInfo attr = new AttributeInfo();
                attr.name = s;
                attr.address = GL.GetAttribLocation(ProgramID, attr.name);
                if (!Attributes.ContainsKey(attr.name))
                    Attributes.Add(attr.name, attr);
            }

        }

        public void GenBuffers()
        {
            uint indexBuffer = 0;
            GL.GenBuffers(1, out indexBuffer);
            Buffers.Add("index", indexBuffer);

            for (int i = 0; i < Attributes.Count; i++)
            {
                uint buffer = 0;
                GL.GenBuffers(1, out buffer);

                Buffers.Add(Attributes.Values.ElementAt(i).name, buffer);
            }
        }

        public void EnableVertexAttribArrays()
        {
            for (int i = 0; i < Attributes.Count; i++)
            {
                GL.EnableVertexAttribArray(Attributes.Values.ElementAt(i).address);
            }
        }

        public void DisableVertexAttribArrays()
        {
            for (int i = 0; i < Attributes.Count; i++)
            {
                GL.DisableVertexAttribArray(Attributes.Values.ElementAt(i).address);
            }
        }

        public int GetAttribute(string name)
        {
            if (Attributes.ContainsKey(name))
            {
                return Attributes[name].address;
            }
            else
            {
                return -1;
            }
        }

        public int GetUniform(string name)
        {
            if (Uniforms.ContainsKey(name))
            {
                return Uniforms[name].address;
            }
            else
            {
                return -1;
            }
        }

        public uint GetBuffer(string name)
        {
            if (Buffers.ContainsKey(name))
            {
                return Buffers[name];
            }
            else
            {
                return 0;
            }
        }



        public ShaderProgram(String vshader, String fshader, bool fromFile = false)
        {
            ProgramID = GL.CreateProgram();

            string vsShaderCode = vshader, fsShaderCode = fshader;

            if (fromFile)
            {
                vsShaderCode = LoadShaderFromFile(vshader, ShaderType.VertexShader);
                fsShaderCode = LoadShaderFromFile(fshader, ShaderType.FragmentShader);
            }
            else
            {
                LoadShaderFromString(vshader, ShaderType.VertexShader);
                LoadShaderFromString(fshader, ShaderType.FragmentShader);
            }

            Link(vsShaderCode, fsShaderCode);
            GenBuffers();
        }

        public ShaderProgram(string vshader, string fshader, string gshader, bool fromFile = false)
        {
            ProgramID = GL.CreateProgram();

            string vsShaderCode = vshader, fsShaderCode = fshader, gsShaderCode = gshader;

            if (fromFile)
            {
                vsShaderCode = LoadShaderFromFile(vshader, ShaderType.VertexShader);
                fsShaderCode = LoadShaderFromFile(fshader, ShaderType.FragmentShader);
                gsShaderCode = LoadShaderFromFile(gshader, ShaderType.GeometryShader);
            }
            else
            {
                LoadShaderFromString(vshader, ShaderType.VertexShader);
                LoadShaderFromString(fshader, ShaderType.FragmentShader);
                LoadShaderFromString(gshader, ShaderType.GeometryShader);
            }

            Link(vsShaderCode, fsShaderCode, gsShaderCode);
            GenBuffers();
        }

        public class UniformInfo
        {
            public String name = "";
            public int address = -1;
            public int size = 0;
            public ActiveUniformType type;
        }

        public class AttributeInfo
        {
            public String name = "";
            public int address = -1;
            public int size = 0;
            public ActiveAttribType type;
        }
}
}
