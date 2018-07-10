using System;
using System.Globalization;
using MvvmCross.Converters;
using WB.Core.BoundedContexts.Interviewer.Views;

namespace WB.UI.Interviewer.Converters
{
    public class TransferingStatusToTextColorConverter : MvxValueConverter<TransferingStatus, int>
    {
        protected override int Convert(TransferingStatus status, Type targetType, object parameter, CultureInfo culture)
        {
            switch (status)
            {
                case TransferingStatus.Failed:
                case TransferingStatus.Aborted:
                case TransferingStatus.CompletedWithErrors:
                    return Resource.Color.sendtosupervisor_error_text;
                case TransferingStatus.Completed:
                    return Resource.Color.sendtosupervisor_complete_text;
                default:
                    return Resource.Color.sendtosupervisor_regular_text;
            }
        }
    }
}
