﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WB.Core.BoundedContexts.Designer.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class ExceptionMessages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal ExceptionMessages() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("WB.Core.BoundedContexts.Designer.Resources.ExceptionMessages", typeof(ExceptionMessages).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Question can&apos;t be linked and cascading at the same time.
        /// </summary>
        internal static string CantBeLinkedAndCascadingAtSameTime {
            get {
                return ResourceManager.GetString("CantBeLinkedAndCascadingAtSameTime", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Question that is used as parent in cascading dropdowns cant be removed before all child questions are removed.
        /// </summary>
        internal static string CantRemoveParentQuestionInCascading {
            get {
                return ResourceManager.GetString("CantRemoveParentQuestionInCascading", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cascading questions can&apos;t have condition expression.
        /// </summary>
        internal static string CascadingCantHaveConditionExpression {
            get {
                return ResourceManager.GetString("CascadingCantHaveConditionExpression", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cascading questions can&apos;t have validation expression.
        /// </summary>
        internal static string CascadingCantHaveValidationExpression {
            get {
                return ResourceManager.GetString("CascadingCantHaveValidationExpression", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Options in cascading question can not have empty ParentValue column.
        /// </summary>
        internal static string CategoricalCascadingOptionsCantContainsEmptyParentValueField {
            get {
                return ResourceManager.GetString("CategoricalCascadingOptionsCantContainsEmptyParentValueField", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Options in cascading question can not have not numeric value in ParentValue column.
        /// </summary>
        internal static string CategoricalCascadingOptionsCantContainsNotDecimalParentValueField {
            get {
                return ResourceManager.GetString("CategoricalCascadingOptionsCantContainsNotDecimalParentValueField", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There is at least one duplicate of Title + Parent Value pairs. List of options should not contain any duplicates of such type..
        /// </summary>
        internal static string CategoricalCascadingOptionsContainsNotUniqueTitleAndParentValuePair {
            get {
                return ResourceManager.GetString("CategoricalCascadingOptionsContainsNotUniqueTitleAndParentValuePair", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Identifier &apos;{0}&apos; from expression &apos;{1}&apos; is not valid question or roster identifier. Question or roster with such an identifier is missing..
        /// </summary>
        internal static string QuestionOrRosterIdentifierIsMissing {
            get {
                return ResourceManager.GetString("QuestionOrRosterIdentifierIsMissing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Question to cascade from should exist in questionnaire.
        /// </summary>
        internal static string ShouldCascadeFromExistingQuestion {
            get {
                return ResourceManager.GetString("ShouldCascadeFromExistingQuestion", resourceCulture);
            }
        }
    }
}
