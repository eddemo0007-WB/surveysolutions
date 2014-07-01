﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace WB.Core.Infrastructure.BaseStructures
{
    public class InterviewEvaluatorPrototype : IInterviewEvaluator
    {

        public InterviewEvaluatorPrototype(Func<Guid, object> getValue)
        {
            this.getValue = getValue;
            this.validations.Add("age", age_GeneratedValidation);

        }
        
        private Func<Guid, object> getValue;

        private Dictionary<string, Func<bool>> validations = new Dictionary<string, Func<bool>>(); 


        private int age {
            get
            {
                //set from template
                Guid questionId = Guid.Parse("111111111111111111111111");
                return (int)getValue(questionId);
            }
        }


        public static object Evaluate()
        {
            return 2 + 2 * 2;
        }

        public Func<bool> GetValidationByVarName(string varName)
        {
            return this.validations.ContainsKey(varName) ? this.validations[varName] : null;
        }


        private List<int> values = new List<int>() { 40, 2 };

        public int Test()
        {
            return this.values.Sum(i => i);
        }


        public bool age_GeneratedValidation()
        {
            return age >= 3;
        }
    }
}