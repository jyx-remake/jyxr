using System.Xml.Serialization;

namespace JyGame
{
	[XmlType("action")]
	public class StoryAction
	{
		[XmlAttribute]
		public string type;

		[XmlAttribute]
		public string value;

		public static StoryAction CreateDialog(Role role, string msg)
		{
			StoryAction storyAction = new StoryAction();
			storyAction.type = "DIALOG";
			storyAction.value = string.Format("{0}#{1}", role.Key, msg);
			return storyAction;
		}
	}
}
