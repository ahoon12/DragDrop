using System.IO;
using System.Xml.Serialization;
using UnityModManagerNet;

namespace DragDrop
{
    public class Settings : UnityModManager.ModSettings
    {
        public bool requireConfirmOnLevelLoaded = false;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            string filepath = GetPath(modEntry);
            try
            {
                using (StreamWriter writer = new StreamWriter(filepath))
                {
                    XmlSerializer serializer = new XmlSerializer(GetType());
                    serializer.Serialize(writer, this);
                }
            }
            catch
            {
            }
        }

        public override string GetPath(UnityModManager.ModEntry modEntry)
        {
            return Path.Combine(modEntry.Path, GetType().Name + ".xml");
        }
    }
}
