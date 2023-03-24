﻿using System.Drawing;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using MvvmCross.Base;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.Enumerator.Properties;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;
using WB.Core.SharedKernels.Enumerator.Services.MapService;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewLoading;
using WB.Core.SharedKernels.Enumerator.Views;
using WB.UI.Shared.Extensions.Services;

namespace WB.UI.Shared.Extensions.ViewModels;

public class InterviewerMapDashboardViewModel : MapDashboardViewModel
{
    protected override InterviewStatus[] InterviewStatuses { get; } = 
    {
        InterviewStatus.Created,
        InterviewStatus.InterviewerAssigned,
        InterviewStatus.Restarted,
        InterviewStatus.RejectedBySupervisor,
        InterviewStatus.Completed,
    };

    public InterviewerMapDashboardViewModel(IPrincipal principal, 
        IViewModelNavigationService viewModelNavigationService, 
        IUserInteractionService userInteractionService, 
        IMapService mapService, 
        IAssignmentDocumentsStorage assignmentsRepository, 
        IPlainStorage<InterviewView> interviewViewRepository, 
        IEnumeratorSettings enumeratorSettings, 
        ILogger logger, 
        IMapUtilityService mapUtilityService, 
        IMvxMainThreadAsyncDispatcher mainThreadAsyncDispatcher 
        ) 
        : base(principal, viewModelNavigationService, userInteractionService, mapService, assignmentsRepository, interviewViewRepository, enumeratorSettings, logger, mapUtilityService, mainThreadAsyncDispatcher)
    {
    }

    public override bool SupportDifferentResponsible => false;
    
    protected override Symbol GetInterviewMarkerSymbol(InterviewView interview)
    {
        Color markerColor;

        switch (interview.Status)
        {
            case InterviewStatus.Created:
            case InterviewStatus.InterviewerAssigned:
            case InterviewStatus.Restarted:    
                markerColor = Color.FromArgb(0x2a, 0x81, 0xcb);
                break;
            case InterviewStatus.Completed:
                markerColor = Color.FromArgb(0x1f,0x95,0x00);
                break;
            case InterviewStatus.RejectedBySupervisor:
                markerColor = Color.FromArgb(0xe4,0x51,0x2b);
                break;
            default:
                markerColor = Color.Yellow;
                break;
        }

        return new CompositeSymbol(new[]
        {
            new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.White, 22), //for contrast
            new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, markerColor, 16)
        });
    }
    
    protected override KeyValuePair<string, object>[] GetAssignmentAttributes(AssignmentDocument assignment)
    {
        var baseAttributes = base.GetAssignmentAttributes(assignment);

        var interviewsByAssignmentCount = assignment.CreatedInterviewsCount ?? 0;
        var interviewsLeftByAssignmentCount = assignment.Quantity.GetValueOrDefault() - interviewsByAssignmentCount;

        bool canCreateInterview = !assignment.Quantity.HasValue || Math.Max(val1: 0, val2: interviewsLeftByAssignmentCount) > 0;

        return baseAttributes.Concat(new[]
        {
            new KeyValuePair<string, object>("can_create", canCreateInterview),
        }).ToArray();
    }

    protected override async Task ShowMapPopup(IdentifyGraphicsOverlayResult identifyResults, MapPoint projectedLocation)
    {
        string id = identifyResults.Graphics[0].Attributes["id"].ToString();
        string title = identifyResults.Graphics[0].Attributes["title"] as string;
        string subTitle = identifyResults.Graphics[0].Attributes["sub_title"] as string;

        var popupTemplate = $"{title}\r\n{subTitle}";
        
        if (string.IsNullOrEmpty(id))
        {
            string interviewId = identifyResults.Graphics[0].Attributes["interviewId"].ToString();
            string interviewKey = identifyResults.Graphics[0].Attributes["interviewKey"].ToString();
            string status = identifyResults.Graphics[0].Attributes["status"].ToString();
            if (!string.IsNullOrWhiteSpace(popupTemplate))
                popupTemplate += $"\r\n{status}";

            CalloutDefinition myCalloutDefinition =
                new CalloutDefinition(interviewKey, popupTemplate)
                {
                    ButtonImage = await new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle,
                            Color.Blue, 25).CreateSwatchAsync(96)
                };

            myCalloutDefinition.OnButtonClick += OnInterviewButtonClick;
            myCalloutDefinition.Tag = interviewId;
            
            MapView.ShowCalloutAt(projectedLocation, myCalloutDefinition);
        }
        else
        {
            var assignmentInfo = identifyResults.Graphics[0].Attributes;
            bool canCreate = (bool)assignmentInfo["can_create"];

            CalloutDefinition myCalloutDefinition = new CalloutDefinition("#" + id, popupTemplate);
            if (canCreate)
            {
                myCalloutDefinition.ButtonImage =
                    await new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, Color.Blue, 25)
                        .CreateSwatchAsync(96);
                myCalloutDefinition.OnButtonClick += tag => CreateFromAssignmentButtonClick(assignmentInfo, tag);
            }

            myCalloutDefinition.Tag = id;
            MapView.ShowCalloutAt(projectedLocation, myCalloutDefinition);
        }
    }

    private void CreateFromAssignmentButtonClick(IDictionary<string, object> assignmentInfo, object calloutTag)
    {
        bool isCreating = assignmentInfo.ContainsKey("creating");
        if (isCreating)
            return;
            
        assignmentInfo["creating"] = true;
        if(calloutTag != null && (Int32.TryParse(calloutTag as string, out int assignmentId)))
        {
            //create interview from assignment
            ViewModelNavigationService.NavigateToCreateAndLoadInterview(assignmentId);
        }
    }
    
    private async void OnInterviewButtonClick(object calloutTag)
    {
        if (calloutTag is string interviewId)
        {
            var interview = interviewViewRepository.GetById(interviewId);
            if (interview != null)
            {
                if (interview.Status == InterviewStatus.Completed)
                {
                    var isReopen = await UserInteractionService.ConfirmAsync(
                        EnumeratorUIResources.Dashboard_Reinitialize_Interview_Message,
                        okButton: UIResources.Yes,
                        cancelButton: UIResources.No);

                    if (!isReopen)
                    {
                        return;
                    }
                }

                await ViewModelNavigationService.NavigateToAsync<LoadingInterviewViewModel, LoadingViewModelArg>(
                    new LoadingViewModelArg
                    {
                        InterviewId = interview.InterviewId
                    }, true);
            }
        }
    }
}