using System.Reflection;
using System.Runtime.InteropServices;
using Android.App;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Supervisor.DataCollection")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Supervisor.DataCollection")]
[assembly: AssemblyCopyright("Copyright © The World Bank 2018")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]

[assembly: MetaData("net.hockeyapp.android.appIdentifier", Value = "80bf6bc0-7188-4591-9213-0d4895a5e041")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("5.22.20.0")]
[assembly: AssemblyVersion("5.22.20.0")]
[assembly: AssemblyFileVersion("5.22.20.0")]
[assembly: AssemblyInformationalVersion("5.22.20 (build 0)")]

// Add some common permissions, these can be removed if not needed
[assembly: UsesPermission(Android.Manifest.Permission.Internet)]
[assembly: UsesPermission(Android.Manifest.Permission.WriteExternalStorage)]
