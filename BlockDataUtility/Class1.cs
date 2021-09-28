using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using BepInEx;
using System.Xml;

using Timberborn.BlockObjectTools;
using Timberborn.BlockSystem;
using Timberborn.ToolSystem;

namespace BlockDataUtility
{
    [BepInPlugin("org.bepinex.plugins.exampleplugin", "BlockDataUtility", "1.0.0.0")]
    [HarmonyPatch]
    public class Patcher : BaseUnityPlugin
    {
        private static List<string> seen = new List<string>();
        private static XmlWriter xmlWriter = XmlWriter.Create("block_output.xml");
        private static IEnumerable<UnityEngine.Vector3Int> GetAllCoordinates(UnityEngine.Vector3Int size)
        {
            int x = 0;
            while (x < size.x)
            {
                int num;
                for (int y = 0; y < size.y; y = num)
                {
                    for (int z = 0; z < size.z; z = num)
                    {
                        yield return new UnityEngine.Vector3Int(x, y, z);
                        num = z + 1;
                    }
                    num = y + 1;
                }
                num = x + 1;
                x = num;
            }
        }
        private static int IndexFromCoordinates(UnityEngine.Vector3Int coordinates, UnityEngine.Vector3Int size)
        {
            return (coordinates.z * size.y + coordinates.y) * size.x + coordinates.x;
        }
        private static void ExportBlockObject(string name, BlockObject instance)
        {
            xmlWriter.WriteStartElement("BlockObject");
            xmlWriter.WriteAttributeString("name", name);
            var size = instance.BlocksSpecification.Size;
            xmlWriter.WriteAttributeString("x", size.x.ToString());
            xmlWriter.WriteAttributeString("y", size.y.ToString());
            xmlWriter.WriteAttributeString("z", size.z.ToString());
            foreach (var coordinate in GetAllCoordinates(size))
            {
                var index = IndexFromCoordinates(coordinate, size);
                var spec = instance.BlocksSpecification.BlockSpecifications[index];
                xmlWriter.WriteStartElement("Block");
                xmlWriter.WriteAttributeString("x", coordinate.x.ToString());
                xmlWriter.WriteAttributeString("y", coordinate.y.ToString());
                xmlWriter.WriteAttributeString("z", coordinate.z.ToString());
                xmlWriter.WriteAttributeString("o", spec.Occupation.ToString());
                xmlWriter.WriteAttributeString("s", spec.Stackable.ToString());
                xmlWriter.WriteAttributeString("m", spec.MatterBelow.ToString());
                xmlWriter.WriteAttributeString("u", spec.OptionallyUnderground.ToString());
                xmlWriter.WriteEndElement();
            }
            if (instance.Entrance.HasEntrance)
            {
                xmlWriter.WriteStartElement("Entrance");
                xmlWriter.WriteAttributeString("x", instance.Entrance.Coordinates.x.ToString());
                xmlWriter.WriteAttributeString("y", instance.Entrance.Coordinates.y.ToString());
                xmlWriter.WriteAttributeString("z", instance.Entrance.Coordinates.z.ToString());
                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();
            xmlWriter.Flush();
        }
        static string getActualInstanceName(string raw_name)
        {
            if (raw_name.Length > 8)
                if (raw_name.Substring(raw_name.Length - 7) == "(Clone)")
                    return raw_name.Substring(0, raw_name.Length - 7);
            return raw_name;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BlockObject), "get_Blocks")]
        public static void GetBlocksOverride(BlockObject __instance)
        {
            string name = getActualInstanceName(__instance.name);
            if (!seen.Contains(name))
            {
                seen.Add(name);
                ExportBlockObject(name, __instance);
            }
        }
        void Awake()
        {
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("BlockObjects");
            var harmony = new Harmony("com.example.patch");
            harmony.PatchAll();
        }
        void OnApplicationQuit()
        {
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }
    }
}
