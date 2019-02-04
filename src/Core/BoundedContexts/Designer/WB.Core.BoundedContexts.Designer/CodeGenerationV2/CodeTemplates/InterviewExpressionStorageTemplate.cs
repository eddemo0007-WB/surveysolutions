﻿// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version: 15.0.0.0
//  
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------
namespace WB.Core.BoundedContexts.Designer.CodeGenerationV2.CodeTemplates
{
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using Main.Core.Entities.SubEntities;
    using System.Text.RegularExpressions;
    using WB.Core.SharedKernels.DataCollection.ExpressionStorage;
    using WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration.Helpers;
    using System;
    
    /// <summary>
    /// Class to produce the template output
    /// </summary>
    
    #line 1 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "15.0.0.0")]
    public partial class InterviewExpressionStorageTemplate : InterviewExpressionStorageTemplateBase
    {
#line hidden
        /// <summary>
        /// Create the template output
        /// </summary>
        public virtual string TransformText()
        {
            this.Write(" ");
            this.Write(@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WB.Core.SharedKernels.DataCollection.ExpressionStorage;
using WB.Core.SharedKernels.DataCollection.Portable;
using WB.Core.SharedKernels.DataCollection.V2.CustomFunctions;
using WB.Core.SharedKernels.DataCollection.V3.CustomFunctions;
using WB.Core.SharedKernels.DataCollection.V4.CustomFunctions;
using WB.Core.SharedKernels.DataCollection.V5.CustomFunctions;
using WB.Core.SharedKernels.DataCollection.ExpressionStorage.CustomFunctions;

/*
	Generated using questionnaire target version ");
            
            #line 24 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.TargetVersion));
            
            #line default
            #line hidden
            this.Write("\r\n*/\r\n\r\nnamespace WB.Core.SharedKernels.DataCollection.Generated\r\n{\r\n\tpublic clas" +
                    "s ");
            
            #line 29 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.ClassName));
            
            #line default
            #line hidden
            this.Write(" : ");
            
            #line 29 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(typeof(IInterviewExpressionStorage).Name));
            
            #line default
            #line hidden
            this.Write(" \r\n\t{\r\n\t\tprivate readonly Dictionary<Guid, Func<RosterVector, ");
            
            #line 31 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.ClassName));
            
            #line default
            #line hidden
            this.Write(", ");
            
            #line 31 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(typeof(IInterviewLevel).Name));
            
            #line default
            #line hidden
            this.Write(">> levelFactory = new Dictionary<Guid, Func<RosterVector, ");
            
            #line 31 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.ClassName));
            
            #line default
            #line hidden
            this.Write(", ");
            
            #line 31 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(typeof(IInterviewLevel).Name));
            
            #line default
            #line hidden
            this.Write(">>();\r\n\t\tpublic ");
            
            #line 32 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.ClassName));
            
            #line default
            #line hidden
            this.Write("()\r\n\t\t{\r\n\t\t\tlevelFactory.Add(IdOf.");
            
            #line 34 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(CodeGeneratorV2.QuestionnaireIdName));
            
            #line default
            #line hidden
            this.Write(", (v, s) => new ");
            
            #line 34 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(CodeGeneratorV2.QuestionnaireLevel));
            
            #line default
            #line hidden
            this.Write("(v, s));\r\n");
            
            #line 35 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"

		foreach (var roster in Model.AllRosters) 
		{

            
            #line default
            #line hidden
            this.Write("\t\t\tlevelFactory.Add(IdOf.");
            
            #line 39 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(roster.Variable));
            
            #line default
            #line hidden
            this.Write(", (v, s) => new ");
            
            #line 39 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(roster.ClassName));
            
            #line default
            #line hidden
            this.Write("(v, s));\r\n");
            
            #line 40 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"

		}

            
            #line default
            #line hidden
            this.Write("\t\r\n\t\t}\r\n\r\n\t\tpublic void Initialize(");
            
            #line 45 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(typeof(IInterviewStateForExpressions).Name));
            
            #line default
            #line hidden
            this.Write(" state) \r\n\t\t{\r\n\t\t\tthis.state = state;\r\n\t\t}\r\n\r\n\t\tinternal ");
            
            #line 50 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(typeof(IInterviewStateForExpressions).Name));
            
            #line default
            #line hidden
            this.Write(" state;\r\n\r\n\t\tpublic ");
            
            #line 52 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(typeof(IInterviewLevel).Name));
            
            #line default
            #line hidden
            this.Write(@"  GetLevel(Identity rosterIdentity)
		{
			return CreateLevel(rosterIdentity);
		}
		
		public RostersCollection<T> GetLevels<T>(Guid levelId, Identity forRosterIdentity) where T : class, IInterviewLevel, IIndexedInterviewLevel
        {
			var rosterIdentities = this.state.FindEntitiesFromSameOrDeeperLevel(levelId, forRosterIdentity);
			var rosters = rosterIdentities.Select(CreateLevel).Cast<T>();
            return new RostersCollection<T>(rosters);
        }

		private ");
            
            #line 64 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(typeof(IInterviewLevel).Name));
            
            #line default
            #line hidden
            this.Write(@" CreateLevel(Identity rosterIdentity)
        {
            if (levelsCache.ContainsKey(rosterIdentity))
                return levelsCache[rosterIdentity];

            if (levelFactory.ContainsKey(rosterIdentity.Id))
            {
                var level = levelFactory[rosterIdentity.Id](rosterIdentity.RosterVector, this);
                levelsCache[rosterIdentity] = level;
                return level;
            }

            return null;
        }

        private readonly Dictionary<Identity, ");
            
            #line 79 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(typeof(IInterviewLevel).Name));
            
            #line default
            #line hidden
            this.Write("> levelsCache = new Dictionary<Identity, ");
            
            #line 79 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(typeof(IInterviewLevel).Name));
            
            #line default
            #line hidden
            this.Write(">();\r\n\t}\r\n\r\n");
            
            #line 82 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"

	foreach (var level in Model.Levels) 
	{
		var template = CreateLevelTemplate(level, Model);
		this.Write(template.TransformText());
	}

            
            #line default
            #line hidden
            this.Write("\r\n\tpublic static class IdOf\r\n\t{\r\n");
            
            #line 92 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"

		foreach (var pair in Model.IdMap)
		{

            
            #line default
            #line hidden
            this.Write("\t\tpublic static readonly Guid ");
            
            #line 96 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(pair.Value));
            
            #line default
            #line hidden
            this.Write(" = ");
            
            #line 96 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(pair.Key.AsBytesString()));
            
            #line default
            #line hidden
            this.Write(";\r\n");
            
            #line 97 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"

		}

            
            #line default
            #line hidden
            this.Write("\t}\r\n\r\n\tpublic class QuestionnaireRandom\r\n\t{\r\n\t\tpublic QuestionnaireRandom(");
            
            #line 104 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(typeof(IInterviewPropertiesForExpressions).Name));
            
            #line default
            #line hidden
            this.Write(" properties)\r\n\t\t{\r\n\t\t\tthis.Properties = properties;\r\n\t\t}\r\n\t\tprivate readonly ");
            
            #line 108 "C:\Work\surveysolutions1\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\CodeGenerationV2\CodeTemplates\InterviewExpressionStorageTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(typeof(IInterviewPropertiesForExpressions).Name));
            
            #line default
            #line hidden
            this.Write(" Properties;\r\n\t\t\t\r\n\t\tpublic double IRnd() => Properties.Random;\r\n\t}\r\n}\r\n");
            return this.GenerationEnvironment.ToString();
        }
    }
    
    #line default
    #line hidden
    #region Base class
    /// <summary>
    /// Base class for this transformation
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "15.0.0.0")]
    public class InterviewExpressionStorageTemplateBase
    {
        #region Fields
        private global::System.Text.StringBuilder generationEnvironmentField;
        private global::System.CodeDom.Compiler.CompilerErrorCollection errorsField;
        private global::System.Collections.Generic.List<int> indentLengthsField;
        private string currentIndentField = "";
        private bool endsWithNewline;
        private global::System.Collections.Generic.IDictionary<string, object> sessionField;
        #endregion
        #region Properties
        /// <summary>
        /// The string builder that generation-time code is using to assemble generated output
        /// </summary>
        protected System.Text.StringBuilder GenerationEnvironment
        {
            get
            {
                if ((this.generationEnvironmentField == null))
                {
                    this.generationEnvironmentField = new global::System.Text.StringBuilder();
                }
                return this.generationEnvironmentField;
            }
            set
            {
                this.generationEnvironmentField = value;
            }
        }
        /// <summary>
        /// The error collection for the generation process
        /// </summary>
        public System.CodeDom.Compiler.CompilerErrorCollection Errors
        {
            get
            {
                if ((this.errorsField == null))
                {
                    this.errorsField = new global::System.CodeDom.Compiler.CompilerErrorCollection();
                }
                return this.errorsField;
            }
        }
        /// <summary>
        /// A list of the lengths of each indent that was added with PushIndent
        /// </summary>
        private System.Collections.Generic.List<int> indentLengths
        {
            get
            {
                if ((this.indentLengthsField == null))
                {
                    this.indentLengthsField = new global::System.Collections.Generic.List<int>();
                }
                return this.indentLengthsField;
            }
        }
        /// <summary>
        /// Gets the current indent we use when adding lines to the output
        /// </summary>
        public string CurrentIndent
        {
            get
            {
                return this.currentIndentField;
            }
        }
        /// <summary>
        /// Current transformation session
        /// </summary>
        public virtual global::System.Collections.Generic.IDictionary<string, object> Session
        {
            get
            {
                return this.sessionField;
            }
            set
            {
                this.sessionField = value;
            }
        }
        #endregion
        #region Transform-time helpers
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void Write(string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
            {
                return;
            }
            // If we're starting off, or if the previous text ended with a newline,
            // we have to append the current indent first.
            if (((this.GenerationEnvironment.Length == 0) 
                        || this.endsWithNewline))
            {
                this.GenerationEnvironment.Append(this.currentIndentField);
                this.endsWithNewline = false;
            }
            // Check if the current text ends with a newline
            if (textToAppend.EndsWith(global::System.Environment.NewLine, global::System.StringComparison.CurrentCulture))
            {
                this.endsWithNewline = true;
            }
            // This is an optimization. If the current indent is "", then we don't have to do any
            // of the more complex stuff further down.
            if ((this.currentIndentField.Length == 0))
            {
                this.GenerationEnvironment.Append(textToAppend);
                return;
            }
            // Everywhere there is a newline in the text, add an indent after it
            textToAppend = textToAppend.Replace(global::System.Environment.NewLine, (global::System.Environment.NewLine + this.currentIndentField));
            // If the text ends with a newline, then we should strip off the indent added at the very end
            // because the appropriate indent will be added when the next time Write() is called
            if (this.endsWithNewline)
            {
                this.GenerationEnvironment.Append(textToAppend, 0, (textToAppend.Length - this.currentIndentField.Length));
            }
            else
            {
                this.GenerationEnvironment.Append(textToAppend);
            }
        }
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void WriteLine(string textToAppend)
        {
            this.Write(textToAppend);
            this.GenerationEnvironment.AppendLine();
            this.endsWithNewline = true;
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void Write(string format, params object[] args)
        {
            this.Write(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void WriteLine(string format, params object[] args)
        {
            this.WriteLine(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Raise an error
        /// </summary>
        public void Error(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Raise a warning
        /// </summary>
        public void Warning(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            error.IsWarning = true;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Increase the indent
        /// </summary>
        public void PushIndent(string indent)
        {
            if ((indent == null))
            {
                throw new global::System.ArgumentNullException("indent");
            }
            this.currentIndentField = (this.currentIndentField + indent);
            this.indentLengths.Add(indent.Length);
        }
        /// <summary>
        /// Remove the last indent that was added with PushIndent
        /// </summary>
        public string PopIndent()
        {
            string returnValue = "";
            if ((this.indentLengths.Count > 0))
            {
                int indentLength = this.indentLengths[(this.indentLengths.Count - 1)];
                this.indentLengths.RemoveAt((this.indentLengths.Count - 1));
                if ((indentLength > 0))
                {
                    returnValue = this.currentIndentField.Substring((this.currentIndentField.Length - indentLength));
                    this.currentIndentField = this.currentIndentField.Remove((this.currentIndentField.Length - indentLength));
                }
            }
            return returnValue;
        }
        /// <summary>
        /// Remove any indentation
        /// </summary>
        public void ClearIndent()
        {
            this.indentLengths.Clear();
            this.currentIndentField = "";
        }
        #endregion
        #region ToString Helpers
        /// <summary>
        /// Utility class to produce culture-oriented representation of an object as a string.
        /// </summary>
        public class ToStringInstanceHelper
        {
            private System.IFormatProvider formatProviderField  = global::System.Globalization.CultureInfo.InvariantCulture;
            /// <summary>
            /// Gets or sets format provider to be used by ToStringWithCulture method.
            /// </summary>
            public System.IFormatProvider FormatProvider
            {
                get
                {
                    return this.formatProviderField ;
                }
                set
                {
                    if ((value != null))
                    {
                        this.formatProviderField  = value;
                    }
                }
            }
            /// <summary>
            /// This is called from the compile/run appdomain to convert objects within an expression block to a string
            /// </summary>
            public string ToStringWithCulture(object objectToConvert)
            {
                if ((objectToConvert == null))
                {
                    throw new global::System.ArgumentNullException("objectToConvert");
                }
                System.Type t = objectToConvert.GetType();
                System.Reflection.MethodInfo method = t.GetMethod("ToString", new System.Type[] {
                            typeof(System.IFormatProvider)});
                if ((method == null))
                {
                    return objectToConvert.ToString();
                }
                else
                {
                    return ((string)(method.Invoke(objectToConvert, new object[] {
                                this.formatProviderField })));
                }
            }
        }
        private ToStringInstanceHelper toStringHelperField = new ToStringInstanceHelper();
        /// <summary>
        /// Helper to produce culture-oriented representation of an object as a string
        /// </summary>
        public ToStringInstanceHelper ToStringHelper
        {
            get
            {
                return this.toStringHelperField;
            }
        }
        #endregion
    }
    #endregion
}
