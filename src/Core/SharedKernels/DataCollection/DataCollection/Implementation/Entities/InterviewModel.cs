﻿using System;
using System.Collections.Generic;
using WB.Core.GenericSubdomains.Utils;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities.QuestionModels;

namespace WB.Core.SharedKernels.DataCollection.Implementation.Entities
{
    public class InterviewModel : IInterview
    {
        public InterviewModel()
        {
            Answers = new Dictionary<string, AbstractInterviewAnswerModel>();
            this.Groups = new Dictionary<string, InterviewGroupModel>();
            RosterInstancesIds = new Dictionary<string, List<string>>();
            QuestionIdToQuestionModelTypeMap = new Dictionary<Guid, Type>();
            IsInProgress = true;
        }

        public string QuestionnaireId { get; set; }
        public long QuestionnaireVersion { get; set; }

        public Guid Id { get; set; }

        public Dictionary<string, AbstractInterviewAnswerModel> Answers { get; set; }
        public Dictionary<string, InterviewGroupModel> Groups { get; set; }
        public Dictionary<string, List<string>> RosterInstancesIds { get; set; }

        public Dictionary<Guid, Type> QuestionIdToQuestionModelTypeMap { get; set; }

        public bool HasErrors { get; set; }
        public bool IsInProgress { get; set; }

        public Dictionary<Type, Func<AbstractInterviewAnswerModel>> QuestionModelTypeToModelActivatorMap = new Dictionary<Type, Func<AbstractInterviewAnswerModel>>
                {
                    { typeof(SingleOptionQuestionModel), () => new SingleOptionAnswerModel()},
                    { typeof(LinkedSingleOptionQuestionModel), () => new LinkedSingleOptionAnswerModel()},
                    { typeof(MultiOptionQuestionModel), () => new MultiOptionAnswerModel()},
                    { typeof(LinkedMultiOptionQuestionModel), () => new LinkedMultiOptionAnswerModel()},
                    { typeof(IntegerNumericQuestionModel), () => new IntegerNumericAnswerModel()},
                    { typeof(RealNumericQuestionModel), () => new RealNumericAnswerModel()},
                    { typeof(MaskedTextQuestionModel), () => new MaskedTextAnswerModel()},
                    { typeof(TextListQuestionModel), () => new TextListAnswerModel()},
                    { typeof(QrBarcodeQuestionModel), () => new QrBarcodeAnswerModel()},
                    { typeof(MultimediaQuestionModel), () => new MultimediaAnswerModel()},
                    { typeof(DateTimeQuestionModel), () => new DateTimeAnswerModel()},
                    { typeof(GpsCoordinatesQuestionModel), () => new GpsCoordinatesAnswerModel()}
                };

        public GpsCoordinatesAnswerModel GetGpsCoordinatesAnswerModel(Identity identity)
        {
            return GetQuestionModel<GpsCoordinatesAnswerModel>(identity);
        }

        public DateTimeAnswerModel GetDateTimeAnswerModel(Identity identity)
        {
            return GetQuestionModel<DateTimeAnswerModel>(identity);
        }

        public MultimediaAnswerModel GetMultimediaAnswerModel(Identity identity)
        {
            return GetQuestionModel<MultimediaAnswerModel>(identity);
        }

        public QrBarcodeAnswerModel GetQrBarcodeAnswerModel(Identity identity)
        {
            return GetQuestionModel<QrBarcodeAnswerModel>(identity);
        }

        public TextListAnswerModel GetTextListAnswerModel(Identity identity)
        {
            return GetQuestionModel<TextListAnswerModel>(identity);
        }

        public LinkedSingleOptionAnswerModel GetLinkedSingleOptionAnswerModel(Identity identity)
        {
            return GetQuestionModel<LinkedSingleOptionAnswerModel>(identity);
        }

        public MultiOptionAnswerModel GetMultiOptionAnswerModel(Identity identity)
        {
            return GetQuestionModel<MultiOptionAnswerModel>(identity);
        }

        public LinkedMultiOptionAnswerModel GetLinkedMultiOptionAnswerModel(Identity identity)
        {
            return GetQuestionModel<LinkedMultiOptionAnswerModel>(identity);
        }

        public IntegerNumericAnswerModel GetIntegerNumericAnswerModel(Identity identity)
        {
            return GetQuestionModel<IntegerNumericAnswerModel>(identity);
        }

        public RealNumericAnswerModel GetRealNumericAnswerModel(Identity identity)
        {
            return GetQuestionModel<RealNumericAnswerModel>(identity);
        }

        public MaskedTextAnswerModel GetTextAnswerModel(Identity identity)
        {
            return GetQuestionModel<MaskedTextAnswerModel>(identity);
        }

        public SingleOptionAnswerModel GetSingleOptionAnswerModel(Identity identity)
        {
            return GetQuestionModel<SingleOptionAnswerModel>(identity);
        }

        private T GetQuestionModel<T>(Identity identity) where T : AbstractInterviewAnswerModel
        {
            var questionId = ConversionHelper.ConvertIdentityToString(identity);
            if (!Answers.ContainsKey(questionId)) return null;
            return (T)Answers[questionId];
        }


        public bool IsValid(Identity identity)
        {
            var questionId = ConversionHelper.ConvertIdentityToString(identity);
            if (!Answers.ContainsKey(questionId))
                return true;

            var interviewAnswerModel = Answers[questionId];
            return !interviewAnswerModel.IsInvalid();
        }

        public bool IsEnabled(Identity entityIdentity)
        {
            var entityKey = ConversionHelper.ConvertIdentityToString(entityIdentity);

            if (this.Groups.ContainsKey(entityKey))
            {
                return !this.Groups[entityKey].IsDisabled;
            }

            if (this.Answers.ContainsKey(entityKey))
            {
                return !this.Answers[entityKey].IsDisabled();
            }

            return true;
        }
    }
}