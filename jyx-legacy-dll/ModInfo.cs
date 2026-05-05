using System.Xml.Serialization;
using JyGame;

[XmlType("mod")]
public class ModInfo : BasePojo
{
	[XmlAttribute]
	public string key;

	[XmlAttribute]
	public string name;

	[XmlAttribute]
	public string author;

	[XmlAttribute]
	public string version;

	[XmlAttribute]
	public string desc;

	[XmlAttribute]
	public string size;

	[XmlAttribute]
	public string date;

	[XmlAttribute]
	public string dir;

	[XmlAttribute]
	public bool enc;

	[XmlAttribute]
	public bool zip;

	public override string PK
	{
		get
		{
			return key;
		}
	}

	[XmlIgnore]
	public string LocalXmlPath
	{
		get
		{
			return CommonSettings.persistentDataPath + "/modcache/" + key + ".xml";
		}
	}

	[XmlIgnore]
	public string LocalDirPath
	{
		get
		{
			return CommonSettings.persistentDataPath + "/modcache/" + key + "/";
		}
	}
}
