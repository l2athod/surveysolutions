﻿// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version: 12.0.0.0
//  
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------
namespace WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration.Templates
{
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using System;
    
    /// <summary>
    /// Class to produce the template output
    /// </summary>
    
    #line 1 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "12.0.0.0")]
    public partial class QuestionnaireLevelTemplate : QuestionnaireLevelTemplateBase
    {
#line hidden
        /// <summary>
        /// Create the template output
        /// </summary>
        public virtual string TransformText()
        {
            this.Write("    private class ");
            
            #line 6 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.GeneratedTypeName));
            
            #line default
            #line hidden
            this.Write(" : AbstractConditionalLevel<");
            
            #line 6 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(this.Model.GeneratedTypeName));
            
            #line default
            #line hidden
            this.Write(">, IExpressionExecutable\r\n    {\r\n        public ");
            
            #line 8 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.GeneratedTypeName));
            
            #line default
            #line hidden
            this.Write(@"(decimal[] rosterVector, Identity[] rosterKey, Func<Identity[], Guid, IEnumerable<IExpressionExecutable>> getInstances, Dictionary<Guid, Guid[]> conditionalDependencies)
            : base(rosterVector, rosterKey, getInstances, conditionalDependencies)
        {
        //TODO: generate mandatory 
        ");
            
            #line 12 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
foreach (var @group in Model.Groups) 
        {
            
            #line default
            #line hidden
            this.Write("            EnablementStates.Add(");
            
            #line 14 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(@group.GeneratedStateName));
            
            #line default
            #line hidden
            this.Write(".ItemId, ");
            
            #line 14 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(@group.GeneratedStateName));
            
            #line default
            #line hidden
            this.Write(");\r\n        ");
            
            #line 15 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
 }
            
            #line default
            #line hidden
            this.Write("\r\n        ");
            
            #line 17 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
foreach (var q in Model.Questions)
        {
            
            #line default
            #line hidden
            this.Write("            EnablementStates.Add(");
            
            #line 19 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(q.GeneratedStateName));
            
            #line default
            #line hidden
            this.Write(".ItemId, ");
            
            #line 19 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(q.GeneratedStateName));
            
            #line default
            #line hidden
            this.Write(");\r\n        ");
            
            #line 20 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
if(!string.IsNullOrWhiteSpace(q.Validations))
        {
            
            #line default
            #line hidden
            this.Write("    \r\n            ValidationExpressions.Add(new Identity(IdOf.");
            
            #line 22 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(q.GeneratedIdName));
            
            #line default
            #line hidden
            this.Write(", rosterVector), new Func<bool>[] { ");
            
            #line 22 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(q.GeneratedValidationsMethodName));
            
            #line default
            #line hidden
            this.Write(" });             \r\n        ");
            
            #line 23 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
 }
            
            #line default
            #line hidden
            this.Write("            AddUpdaterToMap(IdOf.");
            
            #line 24 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(q.GeneratedIdName));
            
            #line default
            #line hidden
            this.Write(", (");
            
            #line 24 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(q.GeneratedTypeName));
            
            #line default
            #line hidden
            this.Write(" val) => {");
            
            #line 24 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(q.GeneratedMemberName));
            
            #line default
            #line hidden
            this.Write("  = val; });\r\n        ");
            
            #line 25 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
 }
            
            #line default
            #line hidden
            this.Write("        ");
            
            #line 26 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
foreach (var @roster in Model.Rosters) 
        {
            
            #line default
            #line hidden
            this.Write("    \r\n            RosterGenerators.Add(IdOf.");
            
            #line 28 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(@roster.GeneratedIdName));
            
            #line default
            #line hidden
            this.Write(", (decimals, identities) => new ");
            
            #line 28 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(@roster.GeneratedTypeName));
            
            #line default
            #line hidden
            this.Write("(rosterVector, identities, this, this.GetInstances, this.ConditionalDependencies)" +
                    ");\r\n        ");
            
            #line 29 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
 }
            
            #line default
            #line hidden
            this.Write("        }                                 \r\n\r\n        public IExpressionExecutabl" +
                    "e CopyMembers(Func<Identity[], Guid, IEnumerable<IExpressionExecutable>> getInst" +
                    "ances)\r\n        {\r\n            var level = new ");
            
            #line 34 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.GeneratedTypeName));
            
            #line default
            #line hidden
            this.Write(@"(this.RosterVector, this.RosterKey, getInstances, ConditionalDependencies)
            {
                ValidAnsweredQuestions = new HashSet<Guid>(this.ValidAnsweredQuestions),
                InvalidAnsweredQuestions = new HashSet<Guid>(this.InvalidAnsweredQuestions),
                ");
            
            #line 38 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
foreach (var q in Model.Questions) 
                {
            
            #line default
            #line hidden
            this.Write("        \r\n                ");
            
            #line 40 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(q.VariableName));
            
            #line default
            #line hidden
            this.Write(" = this.");
            
            #line 40 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(q.GeneratedMemberName));
            
            #line default
            #line hidden
            this.Write(",        \r\n                ");
            
            #line 41 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
}
            
            #line default
            #line hidden
            this.Write(@"            };
                 
            ConditionalDependencies = new Dictionary<Guid, Guid[]>(this.ConditionalDependencies);

            foreach (var state in level.EnablementStates)
            {
                var originalState = this.EnablementStates[state.Key];
                state.Value.PreviousState = originalState.PreviousState;
                state.Value.State = originalState.State;
            }
            
            return level;
        }

        ");
            
            #line 56 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
foreach (var question in Model.Questions) 
        {
            
            #line default
            #line hidden
            this.Write("            \r\n        private ");
            
            #line 58 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(question.GeneratedTypeName));
            
            #line default
            #line hidden
            this.Write(" ");
            
            #line 58 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(question.GeneratedMemberName));
            
            #line default
            #line hidden
            this.Write(";\r\n        private ConditionalState ");
            
            #line 59 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(question.GeneratedStateName));
            
            #line default
            #line hidden
            this.Write(" = new ConditionalState(IdOf.");
            
            #line 59 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(question.GeneratedIdName));
            
            #line default
            #line hidden
            this.Write(");\r\n        public ");
            
            #line 60 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(question.GeneratedTypeName));
            
            #line default
            #line hidden
            this.Write(" ");
            
            #line 60 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(question.VariableName));
            
            #line default
            #line hidden
            this.Write("\r\n        {\r\n            get { return ");
            
            #line 62 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(question.GeneratedStateName));
            
            #line default
            #line hidden
            this.Write(".State == State.Enabled ? this.");
            
            #line 62 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(question.GeneratedMemberName));
            
            #line default
            #line hidden
            this.Write(" : null; }\r\n            private set { this.");
            
            #line 63 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(question.GeneratedMemberName));
            
            #line default
            #line hidden
            this.Write(" = value; }\r\n        }\r\n        ");
            
            #line 65 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
if(!string.IsNullOrWhiteSpace(question.Conditions))
        {
            
            #line default
            #line hidden
            this.Write("        private bool ");
            
            #line 67 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(question.GeneratedConditionsMethodName));
            
            #line default
            #line hidden
            this.Write("()\r\n        {\r\n            return ");
            
            #line 69 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(question.Conditions));
            
            #line default
            #line hidden
            this.Write(";\r\n        }\r\n        ");
            
            #line 71 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
 }
            
            #line default
            #line hidden
            this.Write("        ");
            
            #line 72 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
if(!string.IsNullOrWhiteSpace(question.Validations))
        {
            
            #line default
            #line hidden
            this.Write("        \r\n        private bool ");
            
            #line 74 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(question.GeneratedValidationsMethodName));
            
            #line default
            #line hidden
            this.Write("()\r\n        {\r\n            return ");
            
            #line 76 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(question.Validations));
            
            #line default
            #line hidden
            this.Write(";\r\n        }\r\n        ");
            
            #line 78 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
 }
            
            #line default
            #line hidden
            this.Write("\r\n        ");
            
            #line 80 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
 }
            
            #line default
            #line hidden
            this.Write("\r\n        \r\n        ");
            
            #line 83 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
foreach (var @roster in Model.Rosters) 
        {
            
            #line default
            #line hidden
            this.Write("        public ");
            
            #line 85 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(@roster.GeneratedTypeName));
            
            #line default
            #line hidden
            this.Write("[] ");
            
            #line 85 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(@roster.VariableName));
            
            #line default
            #line hidden
            this.Write("\r\n        {\r\n            get \r\n            {\r\n                var rosters = this." +
                    "GetInstances(new Identity[0], IdOf.");
            
            #line 89 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(@roster.GeneratedRosterScopeName));
            
            #line default
            #line hidden
            this.Write(".Last());\r\n                return rosters == null ? new ");
            
            #line 90 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(@roster.GeneratedTypeName));
            
            #line default
            #line hidden
            this.Write("[0] : rosters.Select(x => x as ");
            
            #line 90 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(@roster.GeneratedTypeName));
            
            #line default
            #line hidden
            this.Write(").ToArray();\r\n            }\r\n        }\r\n        ");
            
            #line 93 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
 }
            
            #line default
            #line hidden
            this.Write("        // groups condition states\r\n        ");
            
            #line 95 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
foreach (var @group in Model.Groups) 
        {
            
            #line default
            #line hidden
            this.Write("        private ConditionalState ");
            
            #line 97 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(@group.GeneratedStateName));
            
            #line default
            #line hidden
            this.Write(" = new ConditionalState(IdOf.");
            
            #line 97 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(@group.GeneratedIdName));
            
            #line default
            #line hidden
            this.Write(", ItemType.Group);\r\n        ");
            
            #line 98 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
 }
            
            #line default
            #line hidden
            this.Write("        //\r\n\r\n        protected override IEnumerable<Action> ConditionExpressions" +
                    "\r\n        {\r\n            get\r\n            {\r\n                return new List<Act" +
                    "ion>\r\n                {\r\n                    ");
            
            #line 107 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
foreach (var tuple in QuestionnaireExecutorTemplateModel.GetOrderedListByConditionDependency(Model.Questions, Model.Groups, null)) 
                    {
            
            #line default
            #line hidden
            this.Write("                    \r\n                    Verifier(");
            
            #line 109 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(tuple.Item1));
            
            #line default
            #line hidden
            this.Write(", ");
            
            #line 109 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(tuple.Item1));
            
            #line default
            #line hidden
            this.Write(".ItemId, ");
            
            #line 109 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(tuple.Item1));
            
            #line default
            #line hidden
            this.Write("),                        \r\n                    ");
            
            #line 110 "C:\Work\WB\Dev\src\Core\BoundedContexts\Designer\WB.Core.BoundedContexts.Designer\Implementation\Services\CodeGeneration\Templates\QuestionnaireLevelTemplate.tt"
}
            
            #line default
            #line hidden
            this.Write(@"                };
            }
        }

        public void CalculateValidationChanges(out List<Identity> questionsToBeValid, out List<Identity> questionsToBeInvalid)
        {
            this.Validate(out questionsToBeValid, out questionsToBeInvalid);
        }
            
        public void SetParent(IExpressionExecutable parentLevel)            
        {            
        }

        public IExpressionExecutable GetParent()
        {
            return null;
        }
    }
");
            return this.GenerationEnvironment.ToString();
        }
    }
    
    #line default
    #line hidden
    #region Base class
    /// <summary>
    /// Base class for this transformation
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "12.0.0.0")]
    public class QuestionnaireLevelTemplateBase
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
