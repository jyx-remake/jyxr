using System.Xml.Serialization;
using JyGame;

[XmlType("gonggao")]
public class GonggaoInfo : BasePojo
{
	[XmlAttribute]
	public string text;

	public override string PK
	{
		get
		{
			return text;
		}
	}
}
