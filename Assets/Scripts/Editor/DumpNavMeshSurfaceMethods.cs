using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;
using System.Reflection;
using System.IO;

public class DumpNavMeshSurfaceMethods
{
    [MenuItem("Tools/Dump NavMeshSurface")]
    public static void Dump()
    {
        var type = typeof(NavMeshSurface);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        using (StreamWriter sw = new StreamWriter("NavMeshSurfaceMethods.txt"))
        {
            foreach (var m in methods)
            {
                sw.WriteLine(m.Name);
            }
        }
        Debug.Log("Dumped to NavMeshSurfaceMethods.txt");
    }
}
