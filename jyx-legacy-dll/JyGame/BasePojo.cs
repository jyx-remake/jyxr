using System.Xml.Serialization;

namespace JyGame
{
	public abstract class BasePojo
	{
		[XmlIgnore]
		public string Xml = string.Empty;

		public abstract string PK { get; }

		public virtual void InitBind()
		{
		}

		public static T Create<T>(string xml) where T : BasePojo
		{
			T val = Tools.LoadObjectFromXML<T>(xml);
			val.InitBind();
			val.Xml = xml;
			return val;
		}
	}
}
