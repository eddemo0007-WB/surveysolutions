using System;
using Machine.Specifications;
using Ncqrs.Eventing.ServiceModel.Bus;
using WB.Core.BoundedContexts.Headquarters.Events.Survey;
using WB.Core.BoundedContexts.Headquarters.Implementation.EventHandlers;
using WB.Core.BoundedContexts.Headquarters.Views.Survey;

namespace WB.Core.BoundedContexts.Headquarters.Tests.SurveyDetailsViewDenormalizerTests
{
    internal class when_applying_NewSurveyStarted_event : SurveyDetailsViewDenormalizerTestsContext
    {
        Establish context = () =>
        {
            @event = ToPublishedEvent(Guid.Parse(surveyShortGuid), new NewSurveyStarted(surveyName));

            denormalizer = CreateSurveyDetailsViewDenormalizer();
        };

        Because of = () =>
            resultView = denormalizer.Update(null, @event);

        It should_return_view_with_id_equal_to_short_survey_guid_representation = () =>
            resultView.SurveyId.ShouldEqual(surveyShortGuid);

        It should_return_view_with_name_equal_to_survey_name = () =>
            resultView.Name.ShouldEqual(surveyName);

        private static SurveyDetailsView resultView;
        private static SurveyDetailsViewDenormalizer denormalizer;
        private static IPublishedEvent<NewSurveyStarted> @event;
        private static string surveyShortGuid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private static string surveyName = "survey name";
    }
}