using System;

namespace SecureSocket
{
	[Serializable()]
	public class Envelope
	{
		object data;
		string envelopetype;		
		Type type;
		public Envelope(string name,object data)
		{
			type=data.GetType();
			this.data=data;
			envelopetype=name;
		}

		public string Name
		{
			get{return envelopetype;}
		}

		public object Data
		{
			get{return data;}
		}
	}
}
