﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WB.Core.BoundedContexts.Headquarter.L.Resources {
    using System;
    using System.Reflection;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class HeadquarterUserCommandValidatorMessages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal HeadquarterUserCommandValidatorMessages() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("WB.Core.BoundedContexts.Headquarter.L.Resources.HeadquarterUserCommandValidatorMe" +
                            "ssages", typeof(HeadquarterUserCommandValidatorMessages).GetTypeInfo().Assembly);
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
        ///   Looks up a localized string similar to Could not update user information because current user does not exist.
        /// </summary>
        internal static string UserDoesNotExist {
            get {
                return ResourceManager.GetString("UserDoesNotExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to User name &apos;{0}&apos; is already taken by an archived user. Please choose a different user name..
        /// </summary>
        internal static string UserNameIsTakenByArchivedUsersFormat {
            get {
                return ResourceManager.GetString("UserNameIsTakenByArchivedUsersFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to User name &apos;{0}&apos; is already taken. Please choose another user name..
        /// </summary>
        internal static string UserNameIsTakenFormat {
            get {
                return ResourceManager.GetString("UserNameIsTakenFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You can&apos;t unarchive interviewer {0} until supervisor is unarchived.
        /// </summary>
        internal static string YouCantUnarchiveInterviewerUntilSupervisorIsArchivedFormat {
            get {
                return ResourceManager.GetString("YouCantUnarchiveInterviewerUntilSupervisorIsArchivedFormat", resourceCulture);
            }
        }
    }
}
