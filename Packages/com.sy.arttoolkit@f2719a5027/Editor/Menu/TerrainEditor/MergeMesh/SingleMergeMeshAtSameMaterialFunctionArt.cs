
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace SYArtToolkit
{
    public class SingleMergeMeshAtSameMaterialFunction
    {
        public static void MergeMesh(List<GameObject> gameObjects, Material material, DefaultAsset assetConfig)
        {
            if (gameObjects.Count <= 0) return;
            List<(Mesh mesh, Matrix4x4 matrix, Vector4 till)> meshes = new List<(Mesh, Matrix4x4, Vector4)>();
            if (gameObjects[0] == null)
            {
                Debug.Log("输入为空");
                return;
            }

            int lightMapIndex = -1;
            Vector4 origin = gameObjects[0].transform.position;
            GameObject originGameObject = gameObjects[0];
            foreach (var gameobj in gameObjects)
            {
                var f = gameobj.GetComponent<MeshFilter>();
                if (f != null && f.sharedMesh != null)
                {
                    meshes.Add((f.sharedMesh, gameobj.transform.localToWorldMatrix,
                        gameobj.GetComponent<MeshRenderer>()
                            ? gameobj.GetComponent<MeshRenderer>().lightmapScaleOffset
                            : new Vector4(1, 1, 0, 0))); //�����ת
                }

                if (lightMapIndex == -1)
                {
                    origin = gameobj.transform.position;
                    originGameObject = gameobj;
                    lightMapIndex = gameobj.GetComponent<MeshRenderer>().lightmapIndex;
                }
            }



            List<List<(Mesh mesh, Matrix4x4 matrix, Vector4 till)>> meshesList =
                new List<List<(Mesh, Matrix4x4, Vector4)>>();
            int sum = 0;
            meshesList.Add(new List<(Mesh mesh, Matrix4x4 matrix, Vector4 till)>());
            foreach (var meshStruct in meshes)
            {
                if (meshStruct.mesh.vertexCount >= 60000)
                {
                    EditorUtility.DisplayDialog("��ʾ", "����mesh������������,�ϲ�ʧ��", "�ر�");
                    return;
                }

                if (sum + meshStruct.mesh.vertexCount >= 60000)
                {
                    meshesList.Add(new List<(Mesh mesh, Matrix4x4 matrix, Vector4 till)>());
                    sum = 0;
                }

                meshesList[meshesList.Count - 1].Add(meshStruct);
                sum += meshStruct.mesh.vertexCount;
            }

            if (meshesList.Count > 1) EditorUtility.DisplayDialog("提示ʾ", "顶带数量过多，分成了" + meshesList.Count + "个Mesh", "确定");
            foreach (var mergeSingleMesh in meshesList)
            {
                Mesh mesh = GetMesh(mergeSingleMesh, origin);
                if (mesh == null || mesh.vertices.Length == 0) continue;
                GameObject t = UnityEngine.Object.Instantiate(originGameObject, null);
                t.name = "newCombineMesh";
                t.transform.position = originGameObject.transform.position;
                t.transform.rotation = new Quaternion(0, 0, 0, 1);
                t.transform.localScale = new Vector3(1, 1, 1);
                t.GetComponent<MeshFilter>().sharedMesh = mesh;
                t.GetComponent<MeshRenderer>().sharedMaterial = material;
                EditorGUIUtility.PingObject(t);
                Selection.activeObject = t;
                string name = "/CombineMesh";
                while (File.Exists(AssetDatabase.GetAssetPath(assetConfig) + name + ".mesh")) name += "0";
                AssetDatabase.CreateAsset(mesh, AssetDatabase.GetAssetPath(assetConfig) + name + ".mesh");
            }
        }

        private struct MeshStructInList
        {
            public List<Vector3> vertices;
            public List<Vector3> normals;
            public List<Vector4> tangents;
            public List<int> triangles;
            public List<Vector2> uv;
            public List<Vector2> uv2;
            public List<Color> color;

            public MeshStructInList(int x)
            {
                vertices = new List<Vector3>();
                normals = new List<Vector3>();
                tangents = new List<Vector4>();
                triangles = new List<int>();
                uv = new List<Vector2>();
                uv2 = new List<Vector2>();
                color = new List<Color>();
            }
        }

        private static Mesh GetMesh(List<(Mesh mesh, Matrix4x4 matrix, Vector4 till)> meshes, Vector4 origin)
        {

            MeshStructInList meshStructInList = new MeshStructInList(0);
            int length = 0;
            foreach (var temporaryMesh in meshes)
            {
                var matrixTemporary = temporaryMesh.matrix;
                var meshTemporary = temporaryMesh.mesh;
                //�ɶ���
                bool readSet = false;
                if (meshTemporary.isReadable == false)
                {
                    readSet = true;
                    string assetPath = AssetDatabase.GetAssetPath(meshTemporary);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                        if (importer)
                        {
                            importer.isReadable = true;
                            importer.SaveAndReimport();
                        }
                    }
                }

                //vertex
                foreach (var meshVertex in meshTemporary.vertices)
                {
                    var v4 = new Vector4(meshVertex.x, meshVertex.y, meshVertex.z, 1);
                    meshStructInList.vertices.Add(matrixTemporary * v4 - origin);
                }

                //trangles
                foreach (var meshtriangle in meshTemporary.triangles)
                {
                    meshStructInList.triangles.Add(meshtriangle + length);
                }

                length += meshTemporary.vertices.Length;

                //normal
                if (meshTemporary.normals.Length == 0)
                {
                    for (int i = 0; i < meshTemporary.vertices.Length; ++i)
                    {
                        meshStructInList.normals.Add(new Vector3(0, 0, 0));
                    }
                }
                else
                {
                    foreach (var nor in meshTemporary.normals)
                    {
                        var matrixIT = matrixTemporary.inverse.transpose;
                        var normalV4 = new Vector4(nor.x, nor.y, nor.z, 0);
                        meshStructInList.normals.Add((matrixIT * normalV4).normalized);
                    }
                }

                //tangents
                if (meshTemporary.tangents.Length == 0)
                {
                    for (int i = 0; i < meshTemporary.vertices.Length; ++i)
                    {
                        meshStructInList.tangents.Add(new Vector4(0, 0, 0, 0));
                    }
                }
                else
                {
                    foreach (var tan in meshTemporary.tangents)
                    {
                        meshStructInList.tangents.Add((matrixTemporary * tan).normalized);
                    }
                }

                //UV
                if (meshTemporary.uv.Length == 0)
                {
                    for (int i = 0; i < meshTemporary.vertices.Length; ++i)
                    {
                        meshStructInList.uv.Add(new Vector2(0, 0));
                    }
                }
                else
                {
                    meshStructInList.uv.AddRange(temporaryMesh.mesh.uv);
                }

                //UV2
                if (meshTemporary.uv2.Length == 0)
                {
                    for (int i = 0; i < meshTemporary.vertices.Length; ++i)
                    {
                        meshStructInList.uv2.Add(new Vector2(0, 0));
                    }
                }
                else
                {
                    var scaleuv2 = new Vector2(temporaryMesh.till.x, temporaryMesh.till.y);
                    var offsetuv2 = new Vector2(temporaryMesh.till.z, temporaryMesh.till.w);
                    foreach (var uv2temp in meshTemporary.uv2)
                    {
                        meshStructInList.uv2.Add(uv2temp * scaleuv2 + offsetuv2);
                    }
                }

                //color
                if (meshTemporary.colors.Length == 0)
                {
                    for (int i = 0; i < meshTemporary.vertices.Length; ++i)
                    {
                        meshStructInList.color.Add(new Color());
                    }
                }
                else
                {
                    meshStructInList.color.AddRange(temporaryMesh.mesh.colors);
                }

                //�ɶ����޸�
                if (readSet)
                {
                    string assetPath = AssetDatabase.GetAssetPath(meshTemporary);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                        if (importer)
                        {
                            importer.isReadable = false;
                            importer.SaveAndReimport();
                        }
                    }
                }

            }

            if (meshStructInList.vertices.Count > 60000)
            {
                EditorUtility.DisplayDialog("��ʾ", "������������", "�ر�");
                return null;
            }

            Mesh mesh = new Mesh();
            mesh.Clear();
            mesh.vertices = meshStructInList.vertices.ToArray();
            mesh.triangles = meshStructInList.triangles.ToArray();
            mesh.normals = meshStructInList.normals.ToArray();
            mesh.uv = meshStructInList.uv.ToArray();
            mesh.uv2 = meshStructInList.uv2.ToArray();
            return mesh;
        }


    }




}
