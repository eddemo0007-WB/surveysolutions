using System;
using Cirrious.MvvmCross.ViewModels;

namespace WB.Core.BoundedContexts.Capi.ViewModel
{
    public class InterviewGeoLocation : MvxNotifyPropertyChanged
    {
        private double latitude;
        public double Latitude
        {
            get { return latitude; }
            set
            {
                latitude = value;
                RaisePropertyChanged(() => Latitude);
            }
        }

        private double longitude;
        public double Longitude
        {
            get { return longitude; }
            set
            {
                longitude = value;
                RaisePropertyChanged(() => Longitude);
            }
        }

        private double accuracy;
        public double Accuracy
        {
            get { return accuracy; }
            set
            {
                accuracy = value;
                RaisePropertyChanged(() => Accuracy);
            }
        }

        private double altitude;
        public double Altitude
        {
            get { return altitude; }
            set
            {
                altitude = value;
                RaisePropertyChanged(() => Altitude);
            }
        }

        private DateTime timestamp;
        public DateTime Timestamp
        {
            get { return timestamp; }
            set
            {
                timestamp = value;
                RaisePropertyChanged(() => Timestamp);
            }
        }
    }
}