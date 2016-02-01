﻿// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version: 14.0.0.0
//  
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------
namespace WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration.V5.Templates
{
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using Main.Core.Entities.SubEntities;
    using System.Text.RegularExpressions;
    using System;
    
    /// <summary>
    /// Class to produce the template output
    /// </summary>
    
    #line 1 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "14.0.0.0")]
    public partial class InterviewExpressionStateTemplateV5 : InterviewExpressionStateTemplateV5Base
    {
#line hidden
        /// <summary>
        /// Create the template output
        /// </summary>
        public virtual string TransformText()
        {
            this.Write("using System;\r\nusing System.Collections.Generic;\r\nusing System.Linq;\r\nusing Syste" +
                    "m.Text.RegularExpressions;\r\n");
            
            #line 12 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"

foreach (var namespaceToInclude in QuestionnaireStructure.Namespaces)
{

            
            #line default
            #line hidden
            this.Write("using ");
            
            #line 16 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(namespaceToInclude));
            
            #line default
            #line hidden
            this.Write(";\r\n");
            
            #line 17 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"

}

            
            #line default
            #line hidden
            this.Write("\r\nnamespace WB.Core.SharedKernels.DataCollection.Generated\r\n{\r\n\tpublic class ");
            
            #line 23 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(QuestionnaireStructure.ClassName));
            
            #line default
            #line hidden
            this.Write(" : AbstractInterviewExpressionStateV5\r\n\t{\r\n\t\tpublic ");
            
            #line 25 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(QuestionnaireStructure.ClassName));
            
            #line default
            #line hidden
            this.Write("() : base()\r\n\t\t{\r\n\t\t\tvar questionnaireLevelScope = new[] { IdOf.@__questionnaire " +
                    "};\r\n\t\t\tvar questionnaireIdentityKey = Util.GetRosterKey(questionnaireLevelScope," +
                    " Util.EmptyRosterVector);\r\n\t\t\tvar questionnaireLevel = new ");
            
            #line 29 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(QuestionnaireStructure.QuestionnaireLevelModel.TypeName));
            
            #line default
            #line hidden
            this.Write(@"(
				Util.EmptyRosterVector, 
				questionnaireIdentityKey, 
				this.GetRosterInstances, 
				IdOf.conditionalDependencies, 
				IdOf.structuralDependencies, 
				this.InterviewProperties);
			this.InterviewScopes.Add(Util.GetRosterStringKey(questionnaireIdentityKey), questionnaireLevel);				
		}

		public ");
            
            #line 39 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(QuestionnaireStructure.ClassName));
            
            #line default
            #line hidden
            this.Write(@"(
			Dictionary<string, IExpressionExecutableV5> interviewScopes, 
			Dictionary<string, List<string>> siblingRosters, 
			IInterviewProperties interviewProperties)
		 :base(interviewScopes, siblingRosters, interviewProperties){}

		protected override bool HasParentScropeRosterId(Guid rosterId){
			return IdOf.parentScopeMap.ContainsKey(rosterId);
		}

		protected override Guid GetQuestionnaireId()
		{
			return IdOf.@__questionnaire;
		}

		protected override Guid[] GetParentRosterScopeIds(Guid rosterId)
		{
			return IdOf.parentScopeMap[rosterId];
		}

		public override IInterviewExpressionState Clone()
		{
			return new ");
            
            #line 61 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(QuestionnaireStructure.ClassName));
            
            #line default
            #line hidden
            this.Write("(this.InterviewScopes, this.SiblingRosters, this.InterviewProperties);\r\n\t\t}\r\n\t\t\r\n" +
                    "\r\n\t\tpublic override Dictionary<Guid, Guid[]> GetParentsMap()\r\n\t\t{\r\n\t\t\treturn IdO" +
                    "f.parentScopeMap;\r\n\t\t}\r\n\t}\r\n\t\t//generate QuestionnaireLevel\r\n");
            
            #line 71 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
	
			QuestionnaireLevelTemplateV5 questionnairetemplate = CreateQuestionnaireLevelTemplate();
			this.Write(questionnairetemplate.TransformText());						   

            
            #line default
            #line hidden
            this.Write("\r\n\t\t//generating rosters\r\n");
            
            #line 77 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"

		foreach (var rosterGroup in QuestionnaireStructure.RostersGroupedByScope) 
		{
			RosterScopeTemplateV5 template = CreateRosterScopeTemplate(rosterGroup.Value);
			this.Write(template.TransformText());
		}

            
            #line default
            #line hidden
            this.Write("\r\n\tpublic static class IdOf\r\n\t{\r\n\t\tpublic static readonly Guid @__questionnaire =" +
                    " Guid.Parse(\"");
            
            #line 87 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(QuestionnaireStructure.Id));
            
            #line default
            #line hidden
            this.Write("\"); \r\n\t\t//questions\r\n");
            
            #line 89 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"

		foreach (var q in QuestionnaireStructure.AllQuestions) 
		{

            
            #line default
            #line hidden
            this.Write("\t\tpublic static readonly Guid ");
            
            #line 93 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(q.IdName));
            
            #line default
            #line hidden
            this.Write(" = Guid.Parse(\"");
            
            #line 93 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(q.Id));
            
            #line default
            #line hidden
            this.Write("\");\r\n");
            
            #line 94 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
 
		}

            
            #line default
            #line hidden
            this.Write("\t\t//groups\r\n");
            
            #line 98 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"

		foreach (var g in QuestionnaireStructure.AllGroups) 
		{

            
            #line default
            #line hidden
            this.Write("\t\tpublic static readonly Guid ");
            
            #line 102 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(g.IdName));
            
            #line default
            #line hidden
            this.Write(" = Guid.Parse(\"");
            
            #line 102 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(g.Id));
            
            #line default
            #line hidden
            this.Write("\");\r\n");
            
            #line 103 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
 
		}

            
            #line default
            #line hidden
            this.Write("\t\t//rosters\r\n");
            
            #line 107 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"

		foreach (var r in QuestionnaireStructure.AllRosters) 
		{

            
            #line default
            #line hidden
            this.Write("\t\tpublic static readonly Guid ");
            
            #line 111 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.IdName));
            
            #line default
            #line hidden
            this.Write(" = Guid.Parse(\"");
            
            #line 111 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.Id));
            
            #line default
            #line hidden
            this.Write("\");\r\n");
            
            #line 112 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
 
		}

            
            #line default
            #line hidden
            this.Write("\t\tpublic static readonly Guid[] ");
            
            #line 115 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(QuestionnaireStructure.QuestionnaireLevelModel.RosterScopeName));
            
            #line default
            #line hidden
            this.Write(" = new[] {@__questionnaire};\r\n\r\n");
            
            #line 117 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"

		foreach (var r in QuestionnaireStructure.AllRosters)
		{

            
            #line default
            #line hidden
            this.Write("\t\tpublic static readonly Guid[] ");
            
            #line 121 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.RosterScopeName));
            
            #line default
            #line hidden
            this.Write(" = new[] {");
            
            #line 121 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(String.Join(" ,", r.RosterScope.Select(g => string.Format("Guid.Parse(\"{0}\")", g)).ToArray())));
            
            #line default
            #line hidden
            this.Write("};\r\n");
            
            #line 122 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
 
		}

            
            #line default
            #line hidden
            this.Write("\t\t\t\r\n\t\tpublic static Dictionary<Guid, Guid[]> conditionalDependencies = new Dicti" +
                    "onary<Guid, Guid[]>()\r\n\t\t{\t\t\t\r\n");
            
            #line 128 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"

			foreach (var dependency in QuestionnaireStructure.ConditionalDependencies)
			{

            
            #line default
            #line hidden
            this.Write("\t\t\t{Guid.Parse(\"");
            
            #line 132 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(dependency.Key));
            
            #line default
            #line hidden
            this.Write("\"), new Guid[]{\r\n");
            
            #line 133 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"

			foreach (var dependencyValue in dependency.Value)
			{

            
            #line default
            #line hidden
            this.Write("\t\t\t\tGuid.Parse(\"");
            
            #line 137 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(dependencyValue));
            
            #line default
            #line hidden
            this.Write("\"),\r\n");
            
            #line 138 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
 
			}

            
            #line default
            #line hidden
            this.Write("\t\t\t}},\r\n");
            
            #line 142 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
 
			}

            
            #line default
            #line hidden
            this.Write("\t\t};\r\n\r\n\t\tpublic static Dictionary<Guid, Guid[]> structuralDependencies = new Dic" +
                    "tionary<Guid, Guid[]>()\r\n\t\t{\r\n");
            
            #line 149 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"

			foreach (var dependency in QuestionnaireStructure.StructuralDependencies) 
			{

            
            #line default
            #line hidden
            this.Write("\t\t\t{ Guid.Parse(\"");
            
            #line 153 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(dependency.Key));
            
            #line default
            #line hidden
            this.Write("\"), new Guid[]{\r\n");
            
            #line 154 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"

			foreach (var d in dependency.Value)
			{

            
            #line default
            #line hidden
            this.Write("\t\t\t\tGuid.Parse(\"");
            
            #line 158 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(d));
            
            #line default
            #line hidden
            this.Write("\"),\r\n");
            
            #line 159 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
 
			}

            
            #line default
            #line hidden
            this.Write("\t\t\t}},\r\n");
            
            #line 163 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"

			}

            
            #line default
            #line hidden
            this.Write("\t\t};\r\n\r\n\t\tpublic static Dictionary<Guid, Guid[]> parentScopeMap = new Dictionary<" +
                    "Guid, Guid[]>\r\n\t\t{\r\n");
            
            #line 170 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
			//questions
			foreach (var q in QuestionnaireStructure.AllQuestions) 
			{

            
            #line default
            #line hidden
            this.Write("\t\t\t{");
            
            #line 174 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(q.IdName));
            
            #line default
            #line hidden
            this.Write(", ");
            
            #line 174 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(q.RosterScopeName));
            
            #line default
            #line hidden
            this.Write("},\r\n");
            
            #line 175 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
 
			}

            
            #line default
            #line hidden
            this.Write("\t\t\t//groups\r\n");
            
            #line 179 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"

			foreach (var g in QuestionnaireStructure.AllGroups) 
			{

            
            #line default
            #line hidden
            this.Write("\t\t\t{");
            
            #line 183 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(g.IdName));
            
            #line default
            #line hidden
            this.Write(", ");
            
            #line 183 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(g.RosterScopeName));
            
            #line default
            #line hidden
            this.Write("},\r\n");
            
            #line 184 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
 
			}

            
            #line default
            #line hidden
            this.Write("\t\t\t//rosters\r\n");
            
            #line 188 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"

			foreach (var r in QuestionnaireStructure.AllRosters)
			{

            
            #line default
            #line hidden
            this.Write("\t\t\t{");
            
            #line 192 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.IdName));
            
            #line default
            #line hidden
            this.Write(", ");
            
            #line 192 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.RosterScopeName));
            
            #line default
            #line hidden
            this.Write("},\r\n");
            
            #line 193 "D:\Projects\SurveySolutions\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\V5\Templates\InterviewExpressionStateTemplateV5.tt"
 
			}

            
            #line default
            #line hidden
            this.Write("\t\t};\r\n\t}\r\n}");
            return this.GenerationEnvironment.ToString();
        }
    }
    
    #line default
    #line hidden
    #region Base class
    /// <summary>
    /// Base class for this transformation
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "14.0.0.0")]
    public class InterviewExpressionStateTemplateV5Base
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
