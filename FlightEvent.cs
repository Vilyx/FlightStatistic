using Pathfinding.Serialization.JsonFx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OLDD
{
	[JsonOptIn]
	class FlightEvent : IEquatable<FlightEvent>, IComparable<FlightEvent>
	{
		[JsonMember]
		public long time;
		[JsonMember]
		private List<FlightEvent> _subsequentEvents;

		public FlightEvent()
		{
			time = EventProcessor.GetTimeInTicks();
		}
		protected List<FlightEvent> subsequentEvents
		{
			get 
			{ 
				if (_subsequentEvents == null)
					_subsequentEvents = new List<FlightEvent>(); 
				return _subsequentEvents; 
			}
			set { _subsequentEvents = value; }
		}

		public void AddEvent(FlightEvent e)
		{
			if (subsequentEvents.Count > 0 && subsequentEvents[subsequentEvents.Count - 1] is EndFlightEvent)
			{
				subsequentEvents.Insert(subsequentEvents.Count - 2, e);
			}
			else
			{
				subsequentEvents.Add(e);
			}
		}

		public virtual bool Revert(long currentTime)
		{
			if (_subsequentEvents == null) return false;
			int removedCount = _subsequentEvents.RemoveAll(x => x.time > currentTime);

			return removedCount != 0;
		}
		public override bool Equals(object obj)
		{
			if (obj == null) return false;
			Part objAsPart = obj as Part;
			if (objAsPart == null) return false;
			else return Equals(objAsPart);
		}
		public int SortByNameAscending(string name1, string name2)
		{

			return name1.CompareTo(name2);
		}

		// Default comparer for Part type.
		public int CompareTo(FlightEvent comparePart)
		{
			// A null value means that this object is greater.
			if (comparePart == null)
				return 1;

			else
				return this.time.CompareTo(comparePart.time);
		}
		public override int GetHashCode()
		{
			try
			{

				return Convert.ToInt32(time);
			}
			catch
			{
				return 0;
			}
		}
		public bool Equals(FlightEvent other)
		{
			if (other == null) return false;
			return (this.time.Equals(other.time));
		}
	}
}
