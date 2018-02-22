﻿using System;
using System.Collections.Generic;
using System.Linq;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform.Core;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.Enumerator.Utils;

namespace WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions.State
{
    public class SpecialValuesViewModel : MvxNotifyPropertyChanged, IDisposable
    {
        private readonly FilteredOptionsViewModel optionsViewModel;
        private readonly IMvxMainThreadDispatcher mvxMainThreadDispatcher;
        private readonly IStatefulInterviewRepository interviewRepository;

        public SpecialValuesViewModel(
            FilteredOptionsViewModel optionsViewModel,
            IMvxMainThreadDispatcher mvxMainThreadDispatcher, 
            IStatefulInterviewRepository interviewRepository)
        {
            this.optionsViewModel = optionsViewModel;
            this.mvxMainThreadDispatcher = mvxMainThreadDispatcher;
            this.interviewRepository = interviewRepository;
            this.SpecialValues = new CovariantObservableCollection<SingleOptionQuestionOptionViewModel>();
            this.allSpecialValues = new HashSet<int>();
        }

        private bool? isSpecialValue;
        private Identity questionIdentity;
        private string interviewId;
        private IQuestionStateViewModel questionState;
        private HashSet<int> allSpecialValues;


        public bool? IsSpecialValue
        {
            get => this.isSpecialValue;
            set
            {
                if (this.isSpecialValue == value) return;
                this.isSpecialValue = value;
                this.RaisePropertyChanged();
            }
        }

        public event EventHandler SpecialValueChanged;
        public event EventHandler SpecialValueRemoved;

        private void SpecialValueSelected(object sender, EventArgs eventArgs)
        {
            var selectedSpecialValue = (SingleOptionQuestionOptionViewModel) sender;
            var previousOption = this.SpecialValues.SingleOrDefault(option => option.Selected && option != selectedSpecialValue);
            
            if (previousOption != null) previousOption.Selected = false;
            
            this.SpecialValueChanged?.Invoke(selectedSpecialValue, EventArgs.Empty);
        }

        public CovariantObservableCollection<SingleOptionQuestionOptionViewModel> SpecialValues { get; private set; }

        public virtual void Init(string interviewId, Identity entityIdentity, IQuestionStateViewModel questionState)
        {
            this.questionIdentity = entityIdentity ?? throw new ArgumentNullException(nameof(entityIdentity));
            this.interviewId = interviewId ?? throw new ArgumentNullException(nameof(interviewId));
            this.questionState = questionState;

            this.optionsViewModel.Init(interviewId, entityIdentity, 200);
            this.UpdateSpecialValues();

            allSpecialValues = this.SpecialValues.Select(x => x.Value).ToHashSet();
        }

        public bool IsSpecialValueSelected(decimal? value)
        {
            return value.HasValue && this.allSpecialValues.Contains(Convert.ToInt32(value.Value));
        }

        private void RemoveAnswerHandler(object sender, EventArgs e)
        {
            this.SpecialValueRemoved?.Invoke(sender, EventArgs.Empty);
        }

        private void UpdateSpecialValues()
        {
            var interview = this.interviewRepository.Get(interviewId);
            var integerQuestion = interview.GetIntegerQuestion(this.questionIdentity);

            var specialValuesViewModels =
                this.optionsViewModel.GetOptions()
                    .Select(model => this.ToViewModel(model, isSelected: integerQuestion.IsAnswered() && model.Value == integerQuestion.GetAnswer().Value))
                    .ToList();

            RemoveSpecialValues();
            specialValuesViewModels.ForEach(x => this.SpecialValues.Add(x));
            this.mvxMainThreadDispatcher.RequestMainThreadAction(() => { this.RaisePropertyChanged(() => this.SpecialValues); });
        }

        private SingleOptionQuestionOptionViewModel ToViewModel(CategoricalOption model, bool isSelected)
        {
            var optionViewModel = new SingleOptionQuestionOptionViewModel
            {
                Enablement = this.questionState.Enablement,
                Value = model.Value,
                Title = model.Title,
                Selected = isSelected,
                QuestionState = this.questionState
            };
            optionViewModel.BeforeSelected += this.SpecialValueSelected;
            optionViewModel.AnswerRemoved += this.RemoveAnswerHandler;

            return optionViewModel;
        }

        public void ClearSelectionAndShowValues()
        {
            if (SpecialValues.Count == 0 && this.allSpecialValues.Any())
            {
                UpdateSpecialValues();
            }
            else
            {
                foreach (var option in this.SpecialValues)
                {
                    option.Selected = false;
                }
            }

            IsSpecialValue = null;
        }

        private void RemoveSpecialValues()
        {
            this.SpecialValues.ForEach(x => x.DisposeIfDisposable());
            this.SpecialValues.Clear();
        }

        public void SetAnswer(decimal? answeredOrSelectedValue)
        {
            IsSpecialValue = IsSpecialValueSelected(answeredOrSelectedValue);

            if (this.IsSpecialValue == true)
            {
                if (SpecialValues.Count == 0 && this.allSpecialValues.Any())
                {
                    UpdateSpecialValues();
                    this.mvxMainThreadDispatcher.RequestMainThreadAction(() => { this.RaisePropertyChanged(() => this.SpecialValues); });
                }

                if (answeredOrSelectedValue.HasValue)
                {
                    var selectedOption = this.SpecialValues.FirstOrDefault(x => x.Value == answeredOrSelectedValue.Value);
                    if (selectedOption != null && selectedOption.Selected == false)
                    {
                        selectedOption.Selected = true;
                    }
                }
            }
            else
            {
                RemoveSpecialValues();
                this.mvxMainThreadDispatcher.RequestMainThreadAction(() => { this.RaisePropertyChanged(() => this.SpecialValues); });
            }
        }

        public void Dispose()
        {
            this.optionsViewModel.Dispose();

            foreach (var option in this.SpecialValues)
            {
                option.BeforeSelected -= this.SpecialValueSelected;
                option.AnswerRemoved -= this.RemoveAnswerHandler;
            }
        }
    }
}