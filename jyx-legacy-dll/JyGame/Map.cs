using System.Collections.Generic;
using System.Xml.Serialization;

namespace JyGame
{
    [XmlType("map")]
    public class Map : BasePojo
    {
        private bool initFlag;

        [XmlAttribute("name")] public string Name;

        [XmlAttribute("pic")] public string Pic;

        [XmlAttribute("desc")] public string Desc;

        [XmlArrayItem(typeof(Music))] [XmlArray("musics")]
        public List<Music> Musics;

        [XmlElement("mapunit")] public List<MapLocation> MapUnits;

        private List<MapLocation> locations = new List<MapLocation>();

        private List<MapRole> mapRoles = new List<MapRole>();

        public override string PK
        {
            get { return Name; }
        }

        public List<MapLocation> Locations
        {
            get { return MapUnits.FindAll(item => item.x >= 0 && item.y >= 0); }
        }

        public List<MapRole> MapRoles
        {
            get { return MapUnits.FindAll(item => item.x < 0 && item.y < 0).ConvertAll(item => item as MapRole); }
        }

        public Music GetRandomMusic()
        {
            if (Musics.Count == 0)
            {
                return null;
            }

            return Musics[Tools.GetRandomInt(0, Musics.Count - 1)];
        }
    }
}