﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WB.UI.Designer.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class PdfMessages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal PdfMessages() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("WB.UI.Designer.Resources.PdfMessages", typeof(PdfMessages).Assembly);
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
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to generate PDF.
        ///Please reload the page and try again or contact support@mysurvey.solutions.
        /// </summary>
        public static string FailedToGenerate {
            get {
                return ResourceManager.GetString("FailedToGenerate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to PDF document generated {0} minute(s) ago.
        ///Size: {1}Kb.
        /// </summary>
        public static string Generate {
            get {
                return ResourceManager.GetString("Generate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to PDF document generated less than a minute ago.
        ///Size: {0}Kb.
        /// </summary>
        public static string GenerateLessMinute {
            get {
                return ResourceManager.GetString("GenerateLessMinute", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Your PDF is being generated.
        ///Size: {0}Kb.
        /// </summary>
        public static string GeneratingSuccess {
            get {
                return ResourceManager.GetString("GeneratingSuccess", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Preparing to generate your PDF.
        ///Please wait....
        /// </summary>
        public static string PreparingToGenerate {
            get {
                return ResourceManager.GetString("PreparingToGenerate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Retring export as PDF..
        /// </summary>
        public static string Retry {
            get {
                return ResourceManager.GetString("Retry", resourceCulture);
            }
        }
    }
}
