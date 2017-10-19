using System;
using System.Diagnostics;

namespace WB.Core.BoundedContexts.Headquarters.Views.Interview
{
    [DebuggerDisplay("{ToString()}")]
    public class InterviewGpsAnswer
    {
        public Guid InterviewId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public override bool Equals(object obj)
        {
            var target = obj as InterviewGpsAnswer;
            if (target == null) return false;

            return this.Equals(target);
        }

        protected bool Equals(InterviewGpsAnswer other) => InterviewId.Equals(other.InterviewId) &&
                                                           Latitude.Equals(other.Latitude) &&
                                                           Longitude.Equals(other.Longitude);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = InterviewId.GetHashCode();
                hashCode = (hashCode * 397) ^ Latitude.GetHashCode();
                hashCode = (hashCode * 397) ^ Longitude.GetHashCode();
                return hashCode;
            }
        }
    }
}